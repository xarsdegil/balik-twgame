using System;
using UnityEngine;

public static class FishingChatSettings
{
    public const float DefaultPlayerCooldownSeconds = 3f;
    public const string DefaultFishCommand = "!fish";
    public const string DefaultFishCountCommand = "!fishcount";
    public const string DefaultCountMessage = "@{user} has caught {total} fish.";
    public const string DefaultNotReadyMessage = "@{user} fishing is not ready yet.";
    public const string DefaultCaughtMessage = "@{user} caught a {fish}! Total: {total}.";
    public const string DefaultEscapedMessage = "@{user} the fish got away. Try again!";
    public const string DefaultQueueFullMessage = "@{user} the fishing queue is full. Try again soon.";

    private const string PlayerCooldownKey = "Fishing.Chat.PlayerCooldownSeconds";
    private const string FishCommandKey = "Fishing.Chat.FishCommand";
    private const string FishCountCommandKey = "Fishing.Chat.FishCountCommand";
    private const string CountMessageKey = "Fishing.Chat.CountMessage";
    private const string NotReadyMessageKey = "Fishing.Chat.NotReadyMessage";
    private const string CaughtMessageKey = "Fishing.Chat.CaughtMessage";
    private const string EscapedMessageKey = "Fishing.Chat.EscapedMessage";
    private const string QueueFullMessageKey = "Fishing.Chat.QueueFullMessage";

    public static float PlayerCooldownSeconds => Mathf.Clamp(
        PlayerPrefs.GetFloat(PlayerCooldownKey, DefaultPlayerCooldownSeconds),
        0f,
        60f);
    public static string FishCommand => NormalizeCommand(PlayerPrefs.GetString(FishCommandKey, DefaultFishCommand), DefaultFishCommand);
    public static string FishCountCommand => NormalizeCommand(PlayerPrefs.GetString(FishCountCommandKey, DefaultFishCountCommand), DefaultFishCountCommand);
    public static string CountMessage => PlayerPrefs.GetString(CountMessageKey, DefaultCountMessage);
    public static string NotReadyMessage => PlayerPrefs.GetString(NotReadyMessageKey, DefaultNotReadyMessage);
    public static string CaughtMessage => PlayerPrefs.GetString(CaughtMessageKey, DefaultCaughtMessage);
    public static string EscapedMessage => PlayerPrefs.GetString(EscapedMessageKey, DefaultEscapedMessage);
    public static string QueueFullMessage => PlayerPrefs.GetString(QueueFullMessageKey, DefaultQueueFullMessage);

    public static void Save(
        float playerCooldownSeconds,
        string fishCommand,
        string fishCountCommand,
        string countMessage,
        string notReadyMessage,
        string caughtMessage,
        string escapedMessage,
        string queueFullMessage)
    {
        PlayerPrefs.SetFloat(PlayerCooldownKey, Mathf.Clamp(playerCooldownSeconds, 0f, 60f));
        PlayerPrefs.SetString(FishCommandKey, NormalizeCommand(fishCommand, DefaultFishCommand));
        PlayerPrefs.SetString(FishCountCommandKey, NormalizeCommand(fishCountCommand, DefaultFishCountCommand));
        PlayerPrefs.SetString(CountMessageKey, UseDefaultIfBlank(countMessage, DefaultCountMessage));
        PlayerPrefs.SetString(NotReadyMessageKey, UseDefaultIfBlank(notReadyMessage, DefaultNotReadyMessage));
        PlayerPrefs.SetString(CaughtMessageKey, UseDefaultIfBlank(caughtMessage, DefaultCaughtMessage));
        PlayerPrefs.SetString(EscapedMessageKey, UseDefaultIfBlank(escapedMessage, DefaultEscapedMessage));
        PlayerPrefs.SetString(QueueFullMessageKey, UseDefaultIfBlank(queueFullMessage, DefaultQueueFullMessage));
    }

    public static string Format(string template, string user, string fish = "", int total = 0)
    {
        return (template ?? string.Empty)
            .Replace("{user}", user ?? string.Empty)
            .Replace("{fish}", fish ?? string.Empty)
            .Replace("{total}", total.ToString());
    }

    private static string NormalizeCommand(string value, string fallback)
    {
        string command = UseDefaultIfBlank(value, fallback).Trim().ToLowerInvariant();
        int whitespace = command.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
        if (whitespace >= 0)
        {
            command = command.Substring(0, whitespace);
        }

        return command.StartsWith("!", StringComparison.Ordinal) ? command : "!" + command;
    }

    private static string UseDefaultIfBlank(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
