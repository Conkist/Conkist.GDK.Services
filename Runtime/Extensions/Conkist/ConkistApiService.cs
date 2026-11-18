using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Conkist.GDK.Services.Backend
{
    /// <summary>
    /// High-level Conkist API service for games (initialize, links, telemetry, game and user data).
    ///
    /// Routes that EXIST on conkist-api today (SdkController):
    ///   POST /v1/sdk/initialize            -> { playerId, sessionToken }   (5-minute session JWT)
    ///   POST /v1/sdk/telemetry             -> 202                          (Bearer)
    ///   POST /v1/sdk/links                 -> { code, deeplink }           (Bearer)
    ///   GET  /v1/sdk/links/{code}/status   -> { status, accountId }        (Bearer)
    ///
    /// Routes marked "PLANNED" below are NOT on the server yet. They return null/false/empty
    /// (HTTP 404) until conkist-api implements them. Do not treat that as a client bug.
    ///
    /// The API has no response envelope: success is the HTTP status, errors are { message }.
    /// Error strategy: expected failures (network, timeout, HTTP error) return null/false/empty
    /// and log. Programming errors (null arguments, malformed JSON) throw.
    /// </summary>
    [Obsolete("Deprecated in 0.2.0, removed in the next minor version. Use Conkist.GDK.Services.Conkist.ConkistBackendService. Most routes of this class do not exist on the server.")]
    public class ConkistApiService : BaseBackendService
    {
        private const string ProductionApiUrl = "https://api.conkist.me";
        private const float REQUEST_TIMEOUT = 30f;
        private const long SessionLifetimeSeconds = 300;   // server issues 5-minute session tokens
        private const long LinkLifetimeSeconds = 900;      // server expires pending links after 15 minutes

        [SerializeField] private string _apiBaseUrl = ProductionApiUrl;

        private HttpClient _httpClient;
        private string _publicKey;
        private string _gameId;
        private string _currentUserId;
        private string _sessionToken;
        private AuthToken _currentAuthToken;

        // Created lazily: Start() runs a frame after AddComponent, and callers (and tests) can call earlier.
        private HttpClient Http => _httpClient ??= new HttpClient { Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT) };

        protected override void OnDestroy()
        {
            _httpClient?.Dispose();
            base.OnDestroy();
        }

        // ==================== Session ====================

        /// <summary>
        /// Opens a session. <paramref name="apiKey"/> is the project's PUBLIC key (the project is
        /// resolved from it on the server). <paramref name="gameId"/> is kept client-side.
        /// POST /v1/sdk/initialize
        /// </summary>
        public async UniTask<bool> InitializeAsync(string apiKey, string gameId)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(gameId))
                throw new ArgumentNullException(nameof(apiKey), "API key and game ID cannot be null");

            _publicKey = apiKey;
            _gameId = gameId;

            var session = await OpenSessionAsync("initialize", playerId: null);
            if (session == null) return false;

            onServiceLog?.Invoke("[ConkistApi] Initialized successfully");
            return true;
        }

        /// <summary>Same as <c>LoginAsync(true)</c>. Overrides the base so parameterless calls work.</summary>
        public override async UniTask<string> LoginAsync() => await LoginAsync(true);

        /// <summary>
        /// The server has no separate login: <see cref="InitializeAsync"/> already issues the
        /// anonymous playerId. This returns it and raises the logged-in event.
        /// </summary>
        public async UniTask<string> LoginAsync(bool anonymous)
        {
            if (string.IsNullOrEmpty(_sessionToken))
            {
                onServiceLog?.Invoke("[ConkistApi] Not initialized. Call InitializeAsync first.");
                return null;
            }

            await UniTask.Yield();
            UserProfile = new BackendUserProfile(_currentUserId);
            onServiceLog?.Invoke($"[ConkistApi] Logged in as {_currentUserId}");
            _onLoggedIn?.Invoke();
            return _currentUserId;
        }

        /// <summary>
        /// The server has no refresh endpoint. Session tokens last 5 minutes, so a refresh
        /// opens a new session for the same playerId. POST /v1/sdk/initialize
        /// </summary>
        public async UniTask<AuthToken> RefreshAuthTokenAsync()
        {
            if (string.IsNullOrEmpty(_currentUserId) || string.IsNullOrEmpty(_publicKey))
            {
                Debug.LogWarning("[ConkistApi] Not logged in. Call InitializeAsync first.");
                return null;
            }

            var session = await OpenSessionAsync("token refresh", _currentUserId);
            return session == null ? null : _currentAuthToken;
        }

        // ==================== Account linking ====================

        /// <summary>
        /// Creates a short code (and deep link) the player scans in Conkist App. The player is
        /// taken from the session token, so <paramref name="gameUserId"/> must be the current player.
        /// POST /v1/sdk/links
        /// </summary>
        public async UniTask<TokenLink> GenerateTokenLinkAsync(string gameId, string gameUserId)
        {
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(gameUserId))
                throw new ArgumentNullException("Game ID and user ID cannot be null");

            var (ok, body) = await CallAsync(HttpMethod.Post, "/v1/sdk/links", new { }, "GenerateTokenLink");
            if (!ok) return null;

            var created = JsonConvert.DeserializeObject<CreateLinkResult>(body);
            onServiceLog?.Invoke("[ConkistApi] Token link generated (expires in 15 min)");
            return new TokenLink
            {
                LinkId = created.Code,
                GameId = gameId,
                GameUserId = gameUserId,
                LinkToken = created.Code,
                ExpiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + LinkLifetimeSeconds,
                IsLinked = false,
                Metadata = new Dictionary<string, object> { { "deeplink", created.Deeplink } }
            };
        }

        /// <summary>
        /// Status of a link created by this player: "pending", "linked" or "expired".
        /// GET /v1/sdk/links/{code}/status
        /// </summary>
        public async UniTask<LinkStatus> GetLinkStatusAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentNullException(nameof(code));

            var (ok, body) = await CallAsync(HttpMethod.Get, $"/v1/sdk/links/{Uri.EscapeDataString(code)}/status", null, "GetLinkStatus");
            return ok ? JsonConvert.DeserializeObject<LinkStatus>(body) : null;
        }

        /// <summary>PLANNED (not on server): list links of a player. GET /v1/sdk/players/{userId}/links</summary>
        public async UniTask<List<TokenLink>> GetLinksAsync(string userId, string gameId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var path = $"/v1/sdk/players/{Uri.EscapeDataString(userId)}/links";
            if (!string.IsNullOrEmpty(gameId)) path += $"?gameId={Uri.EscapeDataString(gameId)}";

            var (ok, body) = await CallAsync(HttpMethod.Get, path, null, "GetLinks");
            return ok ? JsonConvert.DeserializeObject<List<TokenLink>>(body) ?? new List<TokenLink>() : new List<TokenLink>();
        }

        /// <summary>PLANNED (not on server): revoke a link. DELETE /v1/sdk/links/{linkId}</summary>
        public async UniTask<bool> RemoveLinkAsync(string linkId)
        {
            if (string.IsNullOrEmpty(linkId))
                throw new ArgumentNullException(nameof(linkId));

            var (ok, _) = await CallAsync(HttpMethod.Delete, $"/v1/sdk/links/{Uri.EscapeDataString(linkId)}", null, "RemoveLink");
            if (ok) onServiceLog?.Invoke($"[ConkistApi] Link {linkId} removed");
            return ok;
        }

        // ==================== Telemetry ====================

        /// <summary>
        /// Sends one event. The server answers 202 even when the event name is not in the
        /// project's approved list; unapproved events are dropped silently on the server.
        /// POST /v1/sdk/telemetry
        /// </summary>
        public async UniTask<bool> EventTriggerAsync(ConkistGameEvent gameEvent)
        {
            if (gameEvent == null)
                throw new ArgumentNullException(nameof(gameEvent));

            var timestamp = gameEvent.TimestampUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(gameEvent.TimestampUnix).UtcDateTime
                : DateTime.UtcNow;

            var payload = new
            {
                events = new[]
                {
                    new
                    {
                        eventName = gameEvent.EventName,
                        timestamp,
                        properties = gameEvent.Properties ?? new Dictionary<string, object>()
                    }
                }
            };

            var (ok, _) = await CallAsync(HttpMethod.Post, "/v1/sdk/telemetry", payload, "EventTrigger");
            return ok;
        }

        // ==================== Game and user data (PLANNED, not on server) ====================

        /// <summary>PLANNED (not on server): read-only game configuration. GET /v1/sdk/games/{gameId}</summary>
        public async UniTask<GameData> FetchGameDataAsync(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
                throw new ArgumentNullException(nameof(gameId));

            var (ok, body) = await CallAsync(HttpMethod.Get, $"/v1/sdk/games/{Uri.EscapeDataString(gameId)}", null, "FetchGameData");
            return ok ? JsonConvert.DeserializeObject<GameData>(body) : null;
        }

        /// <summary>PLANNED (not on server): player profile and progress. GET /v1/sdk/players/{userId}</summary>
        public async UniTask<UserData> FetchUserDataAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var (ok, body) = await CallAsync(HttpMethod.Get, $"/v1/sdk/players/{Uri.EscapeDataString(userId)}", null, "FetchUserData");
            return ok ? JsonConvert.DeserializeObject<UserData>(body) : null;
        }

        /// <summary>PLANNED (not on server): save player data. PUT /v1/sdk/players/{userId}</summary>
        public async UniTask<bool> SaveUserDataAsync(UserData userData)
        {
            if (userData == null)
                throw new ArgumentNullException(nameof(userData));

            var (ok, _) = await CallAsync(HttpMethod.Put, $"/v1/sdk/players/{Uri.EscapeDataString(userData.UserId ?? string.Empty)}", userData, "SaveUserData");
            if (ok) onServiceLog?.Invoke($"[ConkistApi] User data saved for {userData.UserId}");
            return ok;
        }

        /// <summary>PLANNED (not on server): organization project access. POST /v1/sdk/referral/validate</summary>
        public async UniTask<ValidateReferralTokenResponse> ValidateReferralTokenAsync(
            string projectId,
            string tokenValue,
            string conkistUserId)
        {
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(tokenValue))
                throw new ArgumentNullException("Project ID and token cannot be null");

            var (ok, body) = await CallAsync(HttpMethod.Post, "/v1/sdk/referral/validate", new { projectId, tokenValue, conkistUserId }, "ValidateReferralToken");
            if (!ok) return null;

            onServiceLog?.Invoke("[ConkistApi] Referral token validated for org");
            return JsonConvert.DeserializeObject<ValidateReferralTokenResponse>(body);
        }

        // ==================== Internals ====================

        private async UniTask<InitializeResult> OpenSessionAsync(string operation, string playerId)
        {
            var (ok, body) = await CallAsync(HttpMethod.Post, "/v1/sdk/initialize", new { publicKey = _publicKey, playerId }, operation, authenticated: false);
            if (!ok) return null;

            var session = JsonConvert.DeserializeObject<InitializeResult>(body);
            _currentUserId = session.PlayerId;
            _sessionToken = session.SessionToken;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _currentAuthToken = new AuthToken
            {
                Token = _sessionToken,
                UserId = _currentUserId,
                GameId = _gameId,
                IssuedAtUnix = now,
                ExpiresAtUnix = now + SessionLifetimeSeconds
            };
            return session;
        }

        /// <summary>
        /// One HTTP call. Returns (true, body) on 2xx. HTTP errors, network errors and timeouts
        /// are logged and return (false, ...). Any other exception is a programming error and propagates.
        /// </summary>
        private async UniTask<(bool ok, string body)> CallAsync(
            HttpMethod method, string path, object payload, string operation, bool authenticated = true)
        {
            try
            {
                using var request = new HttpRequestMessage(method, _apiBaseUrl.TrimEnd('/') + path);
                if (payload != null)
                    request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                if (authenticated && !string.IsNullOrEmpty(_sessionToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionToken);

                using var response = await Http.SendAsync(request).AsUniTask();
                var body = await response.Content.ReadAsStringAsync().AsUniTask();

                if (response.IsSuccessStatusCode) return (true, body);

                Debug.LogWarning($"[ConkistApi] {operation} failed ({(int)response.StatusCode}): {ReadMessage(body)}");
                return (false, body);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                Debug.LogError($"[ConkistApi] Network error during {operation}: {ex.Message}");
                return (false, null);
            }
        }

        private static string ReadMessage(string body)
        {
            try { return JObject.Parse(body)["message"]?.ToString() ?? body; }
            catch (JsonException) { return body; }
        }

        private class InitializeResult
        {
            [JsonProperty("playerId")] public string PlayerId { get; set; }
            [JsonProperty("sessionToken")] public string SessionToken { get; set; }
        }

        private class CreateLinkResult
        {
            [JsonProperty("code")] public string Code { get; set; }
            [JsonProperty("deeplink")] public string Deeplink { get; set; }
        }
    }

    // ==================== DTOs ====================

    [Serializable]
    public class GameData
    {
        public string GameId;
        public string Version;
        public Dictionary<string, object> Configuration;
        public long UpdatedAtUnix;
    }

    [Serializable]
    public class UserData
    {
        public string UserId;
        public string Username;
        public Dictionary<string, object> CustomData;
        public Dictionary<string, int> Statistics;
        public long CreatedAtUnix;
        public long LastActiveUnix;
    }

    /// <summary>Client-side view of the server's session token (5-minute lifetime).</summary>
    [Serializable]
    public class AuthToken
    {
        public string Token;
        public string UserId;
        public string GameId;
        public long ExpiresAtUnix;
        public long IssuedAtUnix;
    }

    [Serializable]
    public class TokenLink
    {
        public string LinkId;
        public string GameId;
        public string GameUserId;
        public string ConkistUserId;
        public string LinkToken;
        public long ExpiresAtUnix;
        public bool IsLinked;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>Response of GET /v1/sdk/links/{code}/status. Status: pending, linked or expired.</summary>
    [Serializable]
    public class LinkStatus
    {
        [JsonProperty("status")] public string Status;
        [JsonProperty("accountId")] public long? AccountId;
    }

    [Serializable]
    public class ConkistGameEvent
    {
        public string EventId;
        public string EventName;
        public string UserId;
        public string GameId;
        public Dictionary<string, object> Properties;
        public long TimestampUnix;
        public string SessionId;
    }

    [Serializable]
    public class ValidateReferralTokenResponse
    {
        public bool IsValid;
        public string ProjectId;
        public string ClientId;
        public string AssignedMasterId;
        public string UserGroupId;
        public Dictionary<string, object> OrgCustomizations;
    }
}
