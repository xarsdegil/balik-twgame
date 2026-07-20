using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FishingGame.Twitch
{
    public sealed class TwitchChatClient : MonoBehaviour
    {
        private const string Host = "irc.chat.twitch.tv";
        private const int Port = 6697;
        private const float OutgoingMessageInterval = 1f;

        private readonly ConcurrentQueue<ChatMessage> incoming = new ConcurrentQueue<ChatMessage>();
        private readonly ConcurrentQueue<string> outgoing = new ConcurrentQueue<string>();
        private readonly Dictionary<string, float> lastCommandTimes = new Dictionary<string, float>();
        private readonly object writerLock = new object();
        private CancellationTokenSource cancellation;
        private TcpClient tcpClient;
        private StreamWriter writer;
        private TwitchAuthSession session;
        private float nextOutgoingMessageTime;

        public void Connect(TwitchAuthSession authenticatedSession)
        {
            if (authenticatedSession == null || !authenticatedSession.HasAccessToken ||
                string.IsNullOrWhiteSpace(authenticatedSession.login))
            {
                return;
            }

            Disconnect();
            session = authenticatedSession;
            cancellation = new CancellationTokenSource();
            _ = Task.Run(() => ConnectionLoop(cancellation.Token));
        }

        public void Disconnect()
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            lock (writerLock)
            {
                writer?.Dispose();
                writer = null;
                tcpClient?.Close();
                tcpClient = null;
            }
        }

        private void Update()
        {
            while (incoming.TryDequeue(out ChatMessage message))
            {
                HandleMessage(message);
            }

            if (Time.unscaledTime >= nextOutgoingMessageTime && outgoing.TryDequeue(out string messageText))
            {
                SendRaw("PRIVMSG #" + session.login.ToLowerInvariant() + " :" + messageText);
                nextOutgoingMessageTime = Time.unscaledTime + OutgoingMessageInterval;
            }
        }

        private void HandleMessage(ChatMessage message)
        {
            string command = message.text.Trim().Split(' ')[0].ToLowerInvariant();
            string fishCommand = FishingChatSettings.FishCommand;
            string fishCountCommand = FishingChatSettings.FishCountCommand;
            if (command != fishCommand && command != fishCountCommand)
            {
                return;
            }

            if (lastCommandTimes.TryGetValue(message.userId, out float lastTime) &&
                Time.unscaledTime - lastTime < FishingChatSettings.PlayerCooldownSeconds)
            {
                return;
            }

            lastCommandTimes[message.userId] = Time.unscaledTime;
            if (command == fishCountCommand)
            {
                int count = FishCollectionStore.GetTotalCaught(message.userId);
                SendChat(FishingChatSettings.Format(
                    FishingChatSettings.CountMessage,
                    message.displayName,
                    total: count));
                return;
            }

            FishingController fishing = FindFirstObjectByType<FishingController>();
            if (fishing == null)
            {
                SendChat(FishingChatSettings.Format(
                    FishingChatSettings.NotReadyMessage,
                    message.displayName));
                return;
            }

            bool queued = fishing.QueueFishingAttempt(
                message.userId,
                message.displayName,
                (caught, fishName, total) =>
                {
                    if (caught)
                    {
                        SendChat(FishingChatSettings.Format(
                            FishingChatSettings.CaughtMessage,
                            message.displayName,
                            fishName,
                            total));
                    }
                    else
                    {
                        SendChat(FishingChatSettings.Format(
                            FishingChatSettings.EscapedMessage,
                            message.displayName));
                    }
                });
            if (!queued)
            {
                SendChat(FishingChatSettings.Format(
                    FishingChatSettings.QueueFullMessage,
                    message.displayName));
            }
        }

        private async Task ConnectionLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    RuntimeDiagnostics.Log("Twitch chat: connecting");
                    TcpClient client = new TcpClient();
                    await client.ConnectAsync(Host, Port);
                    SslStream ssl = new SslStream(client.GetStream(), false);
                    await ssl.AuthenticateAsClientAsync(Host);
                    StreamReader reader = new StreamReader(ssl, Encoding.UTF8);
                    StreamWriter chatWriter = new StreamWriter(ssl, new UTF8Encoding(false))
                    {
                        NewLine = "\r\n",
                        AutoFlush = true
                    };

                    lock (writerLock)
                    {
                        tcpClient = client;
                        writer = chatWriter;
                    }

                    SendRaw("CAP REQ :twitch.tv/tags twitch.tv/commands");
                    SendRaw("PASS oauth:" + session.accessToken);
                    SendRaw("NICK " + session.login.ToLowerInvariant());
                    SendRaw("JOIN #" + session.login.ToLowerInvariant());
                    RuntimeDiagnostics.Log("Twitch chat: connected and joined channel");

                    while (!token.IsCancellationRequested)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line == null)
                        {
                            RuntimeDiagnostics.Log("Twitch chat: server closed the connection");
                            break;
                        }

                        if (line.StartsWith("PING", StringComparison.Ordinal))
                        {
                            SendRaw("PONG" + line.Substring(4));
                        }
                        else if (TryParsePrivMsg(line, out ChatMessage message))
                        {
                            incoming.Enqueue(message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (!token.IsCancellationRequested)
                    {
                        RuntimeDiagnostics.Log(
                            "Twitch chat connection error: " + exception.GetType().Name +
                            " - " + exception.Message);
                    }
                }
                finally
                {
                    lock (writerLock)
                    {
                        writer?.Dispose();
                        writer = null;
                        tcpClient?.Close();
                        tcpClient = null;
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    await Task.Delay(5000, token);
                }
            }
        }

        private void SendChat(string message)
        {
            string safeMessage = (message ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
            if (outgoing.Count < 100)
            {
                outgoing.Enqueue(safeMessage);
            }
        }

        private void SendRaw(string line)
        {
            lock (writerLock)
            {
                writer?.WriteLine(line);
            }
        }

        private static bool TryParsePrivMsg(string line, out ChatMessage message)
        {
            message = default;
            int commandIndex = line.IndexOf(" PRIVMSG ", StringComparison.Ordinal);
            int textIndex = commandIndex >= 0 ? line.IndexOf(" :", commandIndex, StringComparison.Ordinal) : -1;
            if (commandIndex < 0 || textIndex < 0)
            {
                return false;
            }

            string tagsPart = line.StartsWith("@", StringComparison.Ordinal)
                ? line.Substring(1, line.IndexOf(' ') - 1)
                : string.Empty;
            string userId = GetTag(tagsPart, "user-id");
            string displayName = GetTag(tagsPart, "display-name");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "viewer";
            }

            message = new ChatMessage(userId, DecodeTag(displayName), line.Substring(textIndex + 2));
            return true;
        }

        private static string GetTag(string tags, string name)
        {
            string prefix = name + "=";
            foreach (string tag in tags.Split(';'))
            {
                if (tag.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return tag.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private static string DecodeTag(string value)
        {
            return value.Replace("\\s", " ").Replace("\\:", ";").Replace("\\\\", "\\");
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private readonly struct ChatMessage
        {
            public ChatMessage(string userId, string displayName, string text)
            {
                this.userId = userId;
                this.displayName = displayName;
                this.text = text;
            }

            public readonly string userId;
            public readonly string displayName;
            public readonly string text;
        }
    }
}
