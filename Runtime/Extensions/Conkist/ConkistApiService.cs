using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Conkist.GDK.Loading;

namespace Conkist.GDK.Services.Backend
{
    /// <summary>
    /// Conkist API Service - integrates GDK with conkist-api for game data,
    /// user management, and account linking.
    /// </summary>
    public class ConkistApiService : BaseBackendService
    {
        private HttpClient _httpClient;
        private string _apiBaseUrl = "https://api.conkist.me/api/v1";
        private string _gameId;
        private string _currentUserId;
        private AuthToken _currentAuthToken;
        private const float REQUEST_TIMEOUT = 30f;

        protected override void Start()
        {
            base.Start();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(REQUEST_TIMEOUT);
        }

        private void OnDestroy()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Initialize connection with conkist-api.
        /// Expected errors: invalid apiKey, network timeout → ApiResponse.Success = false
        /// Extraordinary errors: null references → throw Exception
        /// </summary>
        public async UniTask<bool> InitializeAsync(string apiKey, string gameId)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(gameId))
                throw new ArgumentNullException(nameof(apiKey), "API key and game ID cannot be null");

            try
            {
                _gameId = gameId;
                var request = new { apiKey, gameId };
                var json = JsonUtility.ToJson(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/games/initialize",
                    content
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<bool>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogError($"[ConkistApi] Initialize failed: {apiResponse.Message}");
                    return false;
                }

                onServiceLog?.Invoke("[ConkistApi] Initialized successfully");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error during initialize: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error during initialize: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Anonymous login - get gameUserId for this device/session.
        /// </summary>
        public async UniTask<string> LoginAsync(bool anonymous = true)
        {
            if (string.IsNullOrEmpty(_gameId))
            {
                onServiceLog?.Invoke("[ConkistApi] Not initialized. Call InitializeAsync first.");
                return null;
            }

            try
            {
                var request = new { anonymous, gameId = _gameId };
                var json = JsonUtility.ToJson(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/auth/login",
                    content
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<string>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] Login failed: {apiResponse.Message}");
                    return null;
                }

                _currentUserId = apiResponse.Data;
                UserProfile = new BackendUserProfile { UserId = _currentUserId };
                onServiceLog?.Invoke($"[ConkistApi] Logged in as {_currentUserId}");
                _onLoggedIn?.Invoke();

                return _currentUserId;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error during login: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error during login: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetch server-side game configuration (read-only).
        /// </summary>
        public async UniTask<GameData> FetchGameDataAsync(string gameId)
        {
            if (string.IsNullOrEmpty(gameId))
                throw new ArgumentNullException(nameof(gameId));

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/games/{gameId}"
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<GameData>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] FetchGameData failed: {apiResponse.Message}");
                    return null;
                }

                return apiResponse.Data;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error fetching game data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error fetching game data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetch user profile and progress data.
        /// </summary>
        public async UniTask<UserData> FetchUserDataAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/users/{userId}"
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<UserData>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] FetchUserData failed: {apiResponse.Message}");
                    return null;
                }

                return apiResponse.Data;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error fetching user data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error fetching user data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Save or update user data (profile, progress, customData, statistics).
        /// </summary>
        public async UniTask<bool> SaveUserDataAsync(UserData userData)
        {
            if (userData == null)
                throw new ArgumentNullException(nameof(userData));

            try
            {
                var json = JsonUtility.ToJson(userData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"{_apiBaseUrl}/users/{userData.UserId}",
                    content
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<bool>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] SaveUserData failed: {apiResponse.Message}");
                    return false;
                }

                onServiceLog?.Invoke($"[ConkistApi] User data saved for {userData.UserId}");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error saving user data: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error saving user data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generate time-limited token link for account linking (Game → Conkist App).
        /// User scans this token in Conkist App to connect accounts.
        /// </summary>
        public async UniTask<TokenLink> GenerateTokenLinkAsync(string gameId, string gameUserId)
        {
            if (string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(gameUserId))
                throw new ArgumentNullException("Game ID and user ID cannot be null");

            try
            {
                var request = new { gameId, gameUserId };
                var json = JsonUtility.ToJson(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/links/generate",
                    content
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<TokenLink>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] GenerateTokenLink failed: {apiResponse.Message}");
                    return null;
                }

                onServiceLog?.Invoke($"[ConkistApi] Token link generated (expires in 15 min)");
                return apiResponse.Data;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error generating token link: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error generating token link: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Send game events/telemetry to conkist-api for analytics.
        /// </summary>
        public async UniTask<bool> EventTriggerAsync(GameEvent gameEvent)
        {
            if (gameEvent == null)
                throw new ArgumentNullException(nameof(gameEvent));

            try
            {
                var json = JsonUtility.ToJson(gameEvent);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/events",
                    content
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<bool>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] EventTrigger failed: {apiResponse.Message}");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error sending event: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error sending event: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get all active link tokens for a user (optionally filtered by gameId).
        /// </summary>
        public async UniTask<List<TokenLink>> GetLinksAsync(string userId, string gameId = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            try
            {
                var url = $"{_apiBaseUrl}/users/{userId}/links";
                if (!string.IsNullOrEmpty(gameId))
                    url += $"?gameId={gameId}";

                var response = await _httpClient.GetAsync(url).AsUniTask();
                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<List<TokenLink>>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] GetLinks failed: {apiResponse.Message}");
                    return new List<TokenLink>();
                }

                return apiResponse.Data ?? new List<TokenLink>();
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error fetching links: {ex.Message}");
                return new List<TokenLink>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error fetching links: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Remove/revoke a link token (disconnect account from game).
        /// </summary>
        public async UniTask<bool> RemoveLinkAsync(string linkId)
        {
            if (string.IsNullOrEmpty(linkId))
                throw new ArgumentNullException(nameof(linkId));

            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"{_apiBaseUrl}/links/{linkId}"
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<bool>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] RemoveLink failed: {apiResponse.Message}");
                    return false;
                }

                onServiceLog?.Invoke($"[ConkistApi] Link {linkId} removed");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error removing link: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error removing link: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Refresh time-limited auth token for continued API access.
        /// </summary>
        public async UniTask<AuthToken> RefreshAuthTokenAsync()
        {
            if (string.IsNullOrEmpty(_currentUserId))
            {
                Debug.LogWarning("[ConkistApi] Not logged in. Call LoginAsync first.");
                return null;
            }

            try
            {
                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/auth/refresh",
                    null
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<AuthToken>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] Token refresh failed: {apiResponse.Message}");
                    return null;
                }

                _currentAuthToken = apiResponse.Data;
                return _currentAuthToken;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error refreshing token: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error refreshing token: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validate referral token for organization project access.
        /// Static, project-specific token for group access.
        /// </summary>
        public async UniTask<ValidateReferralTokenResponse> ValidateReferralTokenAsync(
            string projectId,
            string tokenValue,
            string conkistUserId)
        {
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(tokenValue))
                throw new ArgumentNullException("Project ID and token cannot be null");

            try
            {
                var request = new { projectId, tokenValue, conkistUserId };
                var json = JsonUtility.ToJson(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/referral/validate",
                    content
                ).AsUniTask();

                var responseBody = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonUtility.FromJson<ApiResponse<ValidateReferralTokenResponse>>(responseBody);

                if (!apiResponse.Success)
                {
                    Debug.LogWarning($"[ConkistApi] ReferralToken validation failed: {apiResponse.Message}");
                    return null;
                }

                onServiceLog?.Invoke($"[ConkistApi] Referral token validated for org");
                return apiResponse.Data;
            }
            catch (HttpRequestException ex)
            {
                Debug.LogError($"[ConkistApi] Network error validating referral token: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistApi] Unexpected error validating referral token: {ex.Message}");
                throw;
            }
        }
    }

    // ==================== DTOs ====================

    [System.Serializable]
    public class GameData
    {
        public string GameId;
        public string Version;
        public Dictionary<string, object> Configuration;
        public long UpdatedAtUnix;
    }

    [System.Serializable]
    public class UserData
    {
        public string UserId;
        public string Username;
        public Dictionary<string, object> CustomData;
        public Dictionary<string, int> Statistics;
        public long CreatedAtUnix;
        public long LastActiveUnix;
    }

    [System.Serializable]
    public class AuthToken
    {
        public string Token;
        public string UserId;
        public string GameId;
        public long ExpiresAtUnix;
        public long IssuedAtUnix;
    }

    [System.Serializable]
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

    [System.Serializable]
    public class GameEvent
    {
        public string EventId;
        public string EventName;
        public string UserId;
        public string GameId;
        public Dictionary<string, object> Properties;
        public long TimestampUnix;
        public string SessionId;
    }

    [System.Serializable]
    public class ApiResponse<T>
    {
        public bool Success;
        public T Data;
        public string Message;
        public int StatusCode;
        public Dictionary<string, string> Errors;
    }

    [System.Serializable]
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
