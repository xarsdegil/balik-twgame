using System;

namespace FishingGame.Twitch
{
    [Serializable]
    public sealed class TwitchAuthSession
    {
        public string clientId;
        public string accessToken;
        public string refreshToken;
        public string login;
        public string userId;
        public string[] scopes;
        public long savedAtUnixSeconds;

        public bool HasAccessToken =>
            !string.IsNullOrWhiteSpace(clientId) &&
            !string.IsNullOrWhiteSpace(accessToken);

        public bool HasRefreshToken =>
            !string.IsNullOrWhiteSpace(clientId) &&
            !string.IsNullOrWhiteSpace(refreshToken);
    }
}
