using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RuntimeDiagnostics : MonoBehaviour
{
    private const double StallThresholdSeconds = 3.0;
    private const long MaxLogBytes = 4 * 1024 * 1024;
    private static readonly ConcurrentQueue<string> PendingLines = new ConcurrentQueue<string>();
    private static readonly AutoResetEvent WriterSignal = new AutoResetEvent(false);
    private static RuntimeDiagnostics instance;
    private static Thread writerThread;
    private static string logPath;
    private static string currentActivity = "Startup";
    private static long lastHeartbeatTicks;
    private static int stallReported;
    private static volatile bool stopping;

    public static string LogPath => logPath ?? Path.Combine(Application.persistentDataPath, "diagnostics.log");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        logPath = Path.Combine(Application.persistentDataPath, "diagnostics.log");
        RotateLogIfNeeded();
        stopping = false;
        lastHeartbeatTicks = Stopwatch.GetTimestamp();
        writerThread = new Thread(WriterLoop)
        {
            IsBackground = true,
            Name = "Fishing Diagnostics Writer"
        };
        writerThread.Start();

        GameObject diagnosticsObject = new GameObject("Runtime Diagnostics");
        DontDestroyOnLoad(diagnosticsObject);
        instance = diagnosticsObject.AddComponent<RuntimeDiagnostics>();

        Application.logMessageReceivedThreaded += CaptureUnityLog;
        AppDomain.CurrentDomain.UnhandledException += CaptureUnhandledException;
        SceneManager.sceneLoaded += CaptureSceneLoaded;

        Log("=== SESSION START ===");
        Log($"App={Application.productName} {Application.version}; Unity={Application.unityVersion}; " +
            $"OS={SystemInfo.operatingSystem}; GPU={SystemInfo.graphicsDeviceName}; " +
            $"Graphics={SystemInfo.graphicsDeviceType}");
        Log("LogPath=" + logPath);
    }

    public static IDisposable Measure(string activity)
    {
        return new ActivityScope(activity);
    }

    public static void Log(string message)
    {
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                      " [T" + Thread.CurrentThread.ManagedThreadId + "] " +
                      (message ?? string.Empty).Replace("\r", " ").Replace("\n", " | ");
        PendingLines.Enqueue(line);
        WriterSignal.Set();
    }

    private void Update()
    {
        long now = Stopwatch.GetTimestamp();
        long previous = Interlocked.Exchange(ref lastHeartbeatTicks, now);
        if (Interlocked.Exchange(ref stallReported, 0) == 1)
        {
            Log($"MAIN THREAD RESUMED after {ElapsedSeconds(previous, now):0.000}s; " +
                "activity=" + Volatile.Read(ref currentActivity));
        }

        Volatile.Write(ref currentActivity, "Idle/frame loop");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Log("Application focus=" + hasFocus);
    }

    private void OnApplicationPause(bool paused)
    {
        Log("Application paused=" + paused);
    }

    private void OnApplicationQuit()
    {
        Log("=== SESSION END ===");
        Shutdown();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            Shutdown();
        }
    }

    private static void Shutdown()
    {
        if (stopping)
        {
            return;
        }

        stopping = true;
        Application.logMessageReceivedThreaded -= CaptureUnityLog;
        AppDomain.CurrentDomain.UnhandledException -= CaptureUnhandledException;
        SceneManager.sceneLoaded -= CaptureSceneLoaded;
        WriterSignal.Set();
        writerThread?.Join(1000);
        writerThread = null;
        instance = null;
    }

    private static void WriterLoop()
    {
        while (!stopping || !PendingLines.IsEmpty)
        {
            WriterSignal.WaitOne(500);
            CheckForMainThreadStall();
            FlushPendingLines();
        }

        FlushPendingLines();
    }

    private static void CheckForMainThreadStall()
    {
        long heartbeat = Interlocked.Read(ref lastHeartbeatTicks);
        long now = Stopwatch.GetTimestamp();
        double stalledFor = ElapsedSeconds(heartbeat, now);
        if (stalledFor >= StallThresholdSeconds && Interlocked.CompareExchange(ref stallReported, 1, 0) == 0)
        {
            Log($"MAIN THREAD STALL detected; duration>={stalledFor:0.000}s; " +
                "activity=" + Volatile.Read(ref currentActivity));
        }
    }

    private static void FlushPendingLines()
    {
        if (string.IsNullOrWhiteSpace(logPath) || PendingLines.IsEmpty)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? Application.persistentDataPath);
            using StreamWriter output = new StreamWriter(logPath, true);
            while (PendingLines.TryDequeue(out string line))
            {
                output.WriteLine(line);
            }
        }
        catch
        {
            // Diagnostics must never interrupt the game.
        }
    }

    private static void RotateLogIfNeeded()
    {
        try
        {
            if (!File.Exists(logPath) || new FileInfo(logPath).Length < MaxLogBytes)
            {
                return;
            }

            string previousPath = Path.Combine(Application.persistentDataPath, "diagnostics.previous.log");
            if (File.Exists(previousPath))
            {
                File.Delete(previousPath);
            }

            File.Move(logPath, previousPath);
        }
        catch
        {
        }
    }

    private static void CaptureUnityLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log)
        {
            return;
        }

        Log($"UNITY {type}: {condition}" +
            (string.IsNullOrWhiteSpace(stackTrace) ? string.Empty : " | " + stackTrace));
    }

    private static void CaptureUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Log("UNHANDLED EXCEPTION: " + args.ExceptionObject);
    }

    private static void CaptureSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"Scene loaded: {scene.name}; mode={mode}");
    }

    private static double ElapsedSeconds(long start, long end)
    {
        return (end - start) / (double)Stopwatch.Frequency;
    }

    private sealed class ActivityScope : IDisposable
    {
        private readonly string previousActivity;
        private readonly string activity;
        private readonly long startedAt;
        private bool disposed;

        public ActivityScope(string value)
        {
            activity = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            previousActivity = Volatile.Read(ref currentActivity);
            Volatile.Write(ref currentActivity, activity);
            startedAt = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            double elapsed = ElapsedSeconds(startedAt, Stopwatch.GetTimestamp());
            if (elapsed >= 0.05)
            {
                Log($"SLOW OPERATION: {activity}; duration={elapsed:0.000}s");
            }

            Volatile.Write(ref currentActivity, previousActivity);
        }
    }
}
