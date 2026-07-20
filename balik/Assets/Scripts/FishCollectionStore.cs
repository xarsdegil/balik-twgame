using System;
using System.Collections.Generic;
using UnityEngine;

public static class FishCollectionStore
{
    private const string SaveKey = "FishingGame.FishCollections.v1";
    private static CollectionData cachedData;

    public static int RecordCatch(string userId, string login, string fishName)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        CollectionData data = Load();
        PlayerCollection player = data.players.Find(entry => entry.userId == userId);
        if (player == null)
        {
            player = new PlayerCollection { userId = userId };
            data.players.Add(player);
        }

        player.login = login ?? string.Empty;
        player.totalCaught++;
        FishEntry fish = player.fish.Find(entry => entry.name == fishName);
        if (fish == null)
        {
            fish = new FishEntry { name = fishName };
            player.fish.Add(fish);
        }

        fish.count++;
        Save(data);
        return player.totalCaught;
    }

    public static int GetTotalCaught(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return 0;
        }

        PlayerCollection player = Load().players.Find(entry => entry.userId == userId);
        return player != null ? player.totalCaught : 0;
    }

    private static CollectionData Load()
    {
        if (cachedData != null)
        {
            return cachedData;
        }

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        cachedData = string.IsNullOrWhiteSpace(json)
            ? new CollectionData()
            : JsonUtility.FromJson<CollectionData>(json) ?? new CollectionData();
        cachedData.players ??= new List<PlayerCollection>();
        return cachedData;
    }

    private static void Save(CollectionData data)
    {
        using IDisposable diagnostics = RuntimeDiagnostics.Measure("FishCollectionStore.SerializeAndSave");
        cachedData = data;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [Serializable]
    private sealed class CollectionData
    {
        public List<PlayerCollection> players = new List<PlayerCollection>();
    }

    [Serializable]
    private sealed class PlayerCollection
    {
        public string userId;
        public string login;
        public int totalCaught;
        public List<FishEntry> fish = new List<FishEntry>();
    }

    [Serializable]
    private sealed class FishEntry
    {
        public string name;
        public int count;
    }
}
