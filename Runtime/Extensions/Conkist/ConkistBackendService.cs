using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using Conkist.GDK.Services.Backend;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Conkist Backend Service — the main entry point for the Conkist SDK integration.
    /// Extends BaseBackendService following the same pattern as UnityBackendService and PlayfabBackendService.
    /// 
    /// Provides 3 core functionalities:
    /// 1. API Key based initialization (POST /v1/sdk/initialize)
    /// 2. Connection token display with QR code for mobile app account linking
    /// 3. Buffered telemetry event tracking with 10-second batch flush
    /// 
    /// SECURITY NOTE: Only the Public API Key (pk_live_...) should be used.
    /// Never embed the Secret API Key (sk_live_...) in client code.
    /// </summary>
    public class ConkistBackendService : BaseBackendService
    {
        private const string ProductionApiUrl = "https://api.conkist.me";
        private const string DeepLinkScheme = "conkist://link-player";
        private const int TokenLifetimeMinutes = 5;
        private const int MaxRetryAttempts = 5;

        [Space, Header("Conkist SDK Configuration")]
        [Tooltip("The Public API Key for your Conkist project (pk_live_...). Never use Secret Keys in client code.")]
        [SerializeField] private string _publicKey;

        private string _secretKey;

        [Tooltip("Debug API URL. Only used in Development Builds. Leave empty to use production (api.conkist.me).")]
        [SerializeField] private string _debugApiUrl;

        [Space, Header("Telemetry Settings")]
        [Tooltip("Interval in seconds between automatic batch flushes.")]
        [SerializeField] private float _flushInterval = 10f;

        [Tooltip("Maximum number of events per batch.")]
        [SerializeField] private int _maxBatchSize = 10;

        [Tooltip("Maximum number of events to hold in memory when offline.")]
        [SerializeField] private int _maxBufferCapacity = 1000;

        [Space, Header("Events")]
        [SerializeField] private UnityEvent _onInitialized;
        [SerializeField] private UnityEvent _onInitializationFailed;

        // C# Events
        public event Action OnSessionInitialized;
        public event Action<string> OnConnectionCodeReceived;

        // Internal state
        private ConkistApiClient _apiClient;
        private ConkistTelemetryBuffer _buffer;
        private ConkistSessionData _session;
        private Texture2D _cachedQRCode;
        private bool _isInitialized;
        private bool _isFlushRunning;

        /// <summary>
        /// Whether the SDK has been successfully initialized with a valid session.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// The current session token string. Null if not initialized.
        /// </summary>
        public string ConnectionToken => _session?.SessionToken;

        /// <summary>
        /// The player ID assigned by the Conkist API. Null if not initialized.
        /// </summary>
        public string PlayerId => _session?.PlayerId;

        /// <summary>
        /// The short linking code returned by the API. Null if not retrieved.
        /// </summary>
        public string ConnectionCode { get; private set; }

        /// <summary>
        /// The deep-link URL returned by the API. Null if not retrieved.
        /// </summary>
        public string ConnectionDeeplink { get; private set; }

        /// <summary>
        /// The UTC time when the session token was last refreshed.
        /// </summary>
        public DateTime LastRefreshTime { get; private set; }

        /// <summary>
        /// The expected UTC time when the session token needs to be refreshed (30s before expiration).
        /// </summary>
        public DateTime NextRefreshTime { get; private set; }

        /// <summary>
        /// The effective API URL based on build type.
        /// Uses _debugApiUrl in development builds (if provided), otherwise production.
        /// </summary>
        private string EffectiveApiUrl
        {
            get
            {
                if (Debug.isDebugBuild && !string.IsNullOrEmpty(_debugApiUrl))
                    return _debugApiUrl.TrimEnd('/');
                return ProductionApiUrl;
            }
        }

        // ─────────────────────────────────────────────
        // Setup & Lifecycle
        // ─────────────────────────────────────────────

        /// <summary>
        /// Configures the service programmatically. Can be called before Start().
        /// </summary>
        /// <param name="publicKey">The public API key (pk_live_...).</param>
        /// <param name="secretKey">The secret API key (sk_live_...). Can be stored during session or persisted.</param>
        /// <param name="debugApiUrl">Optional debug API URL for development builds.</param>
        /// <param name="persistSecret">If true, persists the secret key across restarts using PlayerPrefs.</param>
        public void Setup(string publicKey, string secretKey = null, string debugApiUrl = null, bool persistSecret = false)
        {
            _publicKey = publicKey;
            _secretKey = secretKey;
            _debugApiUrl = debugApiUrl;
        }

        protected override void Start()
        {
            _buffer = new ConkistTelemetryBuffer
            {
                MaxBatchSize = _maxBatchSize,
                MaxCapacity = _maxBufferCapacity
            };
            _session = new ConkistSessionData();
            _apiClient = new ConkistApiClient(EffectiveApiUrl);

            if (_autoLogin)
                LoginAsync().Forget();
        }

        protected override void OnDestroy()
        {
            _isFlushRunning = false;
            base.OnDestroy();
        }

        // ─────────────────────────────────────────────
        // 1. INITIALIZATION (API Key based)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Initializes the SDK session using the configured Public API Key.
        /// Calls POST /v1/sdk/initialize to validate the key and obtain a PlayerId + SessionToken.
        /// </summary>
        /// <returns>The PlayerId on success, null on failure.</returns>
        public override async UniTask<string> LoginAsync()
        {
            if (string.IsNullOrEmpty(_publicKey))
            {
                Debug.LogError("[ConkistSDK] Public Key is not configured. Set it via Inspector or Setup().");
                onError?.Invoke();
                _onInitializationFailed?.Invoke();
                return null;
            }

            onServiceLog?.Invoke($"Initializing with API: {EffectiveApiUrl}");

            var savedPlayerId = PlayerPrefs.GetString("Conkist_PlayerId", null);
            var response = await _apiClient.InitializeSessionAsync(_publicKey, savedPlayerId);

            if (response.IsSuccess && response.Data != null)
            {
                _session.PlayerId = response.Data.PlayerId;
                
                // Persist the PlayerId locally so it can be reused in future sessions
                PlayerPrefs.SetString("Conkist_PlayerId", _session.PlayerId);
                PlayerPrefs.Save();
                _session.SessionToken = response.Data.SessionToken;
                _session.ExpiresAt = DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes);
                UpdateSessionTimes(_session.ExpiresAt);
                _isInitialized = true;

                UserProfile = new BackendUserProfile(_session.PlayerId);

                onServiceLog?.Invoke($"Session initialized. PlayerId: {_session.PlayerId}");
                Debug.Log($"[ConkistSDK] Session initialized. PlayerId: {_session.PlayerId}");

                _onLoggedIn?.Invoke();
                _onInitialized?.Invoke();
                OnSessionInitialized?.Invoke();

                // Generate QR code after successful initialization
                GenerateConnectionQRCodeAsync().Forget();

                // Start the telemetry flush loop
                StartFlushLoop().Forget();

                return _session.PlayerId;
            }
            else
            {
                string errorMsg = response.IsUnauthorized
                    ? "Invalid Public API Key."
                    : $"Initialization failed ({response.StatusCode}): {response.ErrorMessage}";

                Debug.LogError($"[ConkistSDK] {errorMsg}");
                onServiceLog?.Invoke(errorMsg);
                onError?.Invoke();
                _onInitializationFailed?.Invoke();
                return null;
            }
        }

        /// <summary>
        /// Proactively refreshes the session token if it is expiring soon or expired.
        /// Called automatically before each telemetry batch flush.
        /// </summary>
        private async UniTask<bool> RefreshSessionIfNeeded()
        {
            if (_session == null || !_session.IsValid) return false;
            if (!_session.IsExpiringSoon && !_session.IsExpired) return true;

            onServiceLog?.Invoke("Token expiring soon, refreshing session...");

            var savedPlayerId = PlayerPrefs.GetString("Conkist_PlayerId", null);
            var response = await _apiClient.InitializeSessionAsync(_publicKey, savedPlayerId);

            if (response.IsSuccess && response.Data != null)
            {
                _session.SessionToken = response.Data.SessionToken;
                _session.ExpiresAt = DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes);
                UpdateSessionTimes(_session.ExpiresAt);

                // Keep the same PlayerId — the API may assign a new one but we maintain continuity
                if (!string.IsNullOrEmpty(response.Data.PlayerId))
                    _session.PlayerId = response.Data.PlayerId;

                onServiceLog?.Invoke("Session token refreshed successfully.");
                return true;
            }

            Debug.LogWarning($"[ConkistSDK] Token refresh failed: {response.ErrorMessage}");
            return false;
        }

        /// <summary>
        /// Public endpoint to check and proactively refresh the session token.
        /// </summary>
        public async UniTask<bool> CheckAndRefreshSessionAsync()
        {
            return await RefreshSessionIfNeeded();
        }

        // ─────────────────────────────────────────────
        // 2. CONNECTION TOKEN & QR CODE
        // ─────────────────────────────────────────────

        /// <summary>
        /// Generates a QR code texture encoding a deep-link URL for the Conkist mobile app.
        /// The deep-link format: conkist://link-player?token={sessionToken}&playerId={playerId}
        /// 
        /// The mobile app should:
        /// 1. Parse the deep-link and extract the token
        /// 2. Check if the user is authenticated
        /// 3. If not authenticated, redirect to login screen
        /// 4. If authenticated, link the anonymous player to the user's Conkist account
        /// </summary>
        [Obsolete("Use GetConnectionQRCodeAsync instead for async token generation.")]
        public Texture2D GetConnectionQRCode(int size = 8)
        {
            if (_cachedQRCode != null)
                return _cachedQRCode;

            Debug.LogWarning("[ConkistSDK] Synchronous GetConnectionQRCode is deprecated and not supported. Use GetConnectionQRCodeAsync instead.");
            return null;
        }

        /// <summary>
        /// Generates a QR code texture encoding a deep-link URL for the Conkist mobile app.
        /// The deep-link format: conkist://connect?token={shortCode}
        /// </summary>
        /// <param name="size">Pixels per QR module (default 8, resulting in ~232px for v1).</param>
        /// <returns>The generated QR code Texture2D, or null if not initialized.</returns>
        public async UniTask<Texture2D> GetConnectionQRCodeAsync(int size = 8)
        {
            if (!_isInitialized || _session == null || !_session.IsValid)
            {
                Debug.LogWarning("[ConkistSDK] Cannot generate QR code: session not initialized.");
                return null;
            }

            if (_cachedQRCode != null)
                return _cachedQRCode;

            return await GenerateConnectionQRCodeAsync(size);
        }

        private async UniTask<Texture2D> GenerateConnectionQRCodeAsync(int pixelsPerModule = 8)
        {
            if (_session == null || !_session.IsValid) return null;

            onServiceLog?.Invoke("Requesting short link token from backend...");
            var response = await _apiClient.CreateLinkRequestAsync(_session.SessionToken);

            if (response.IsSuccess && response.Data != null)
            {
                string deepLink = response.Data.Deeplink;
                ConnectionCode = response.Data.Code;
                ConnectionDeeplink = response.Data.Deeplink;
                _cachedQRCode = QRCodeGenerator.Generate(deepLink, pixelsPerModule);

                if (_cachedQRCode != null)
                {
                    LastRefreshTime = DateTime.UtcNow;
                    NextRefreshTime = _session.ExpiresAt.AddSeconds(-30);

                    onServiceLog?.Invoke($"QR code generated for account linking. Code: {response.Data.Code}");
                    OnConnectionCodeReceived?.Invoke(ConnectionCode);
                }
                else
                {
                    Debug.LogWarning("[ConkistSDK] QR code generation failed.");
                }
            }
            else
            {
                string errorMsg = $"Link request failed ({response.StatusCode}): {response.ErrorMessage}";
                Debug.LogError($"[ConkistSDK] {errorMsg}");
                onServiceLog?.Invoke(errorMsg);
            }

            return _cachedQRCode;
        }

        /// <summary>
        /// Checks the status of the current connection code (polling endpoint).
        /// </summary>
        public async UniTask<ApiResponse<GetLinkStatusResponse>> CheckLinkStatusAsync()
        {
            if (!_isInitialized || _session == null || !_session.IsValid || string.IsNullOrEmpty(ConnectionCode))
            {
                return ApiResponse<GetLinkStatusResponse>.Failure(400, "SDK not initialized or Connection Code missing.");
            }

            return await _apiClient.GetLinkStatusAsync(ConnectionCode, _session.SessionToken);
        }

        /// <summary>
        /// Invalidates the cached QR code so the next call to GetConnectionQRCode() regenerates it.
        /// Call this after a token refresh to get an updated QR code.
        /// </summary>
        public void InvalidateQRCode()
        {
            ConnectionCode = null;
            ConnectionDeeplink = null;
            LastRefreshTime = default(DateTime);
            NextRefreshTime = default(DateTime);
            if (_cachedQRCode != null)
            {
                Destroy(_cachedQRCode);
                _cachedQRCode = null;
            }
        }

        private void UpdateSessionTimes(DateTime expiresAt)
        {
            LastRefreshTime = DateTime.UtcNow;
            NextRefreshTime = expiresAt.AddSeconds(-30);
        }

        // ─────────────────────────────────────────────
        // 3. TELEMETRY EVENT TRACKING
        // ─────────────────────────────────────────────

        /// <summary>
        /// Enqueues a telemetry event for batch transmission.
        /// Events are NOT sent immediately — they are buffered and flushed every FlushInterval seconds.
        /// </summary>
        /// <param name="eventName">The event name (must match an approved event in the project's governance dictionary).</param>
        /// <param name="properties">Optional key-value properties for the event.</param>
        public void TrackEvent(string eventName, Dictionary<string, object> properties = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[ConkistSDK] TrackEvent called with null/empty eventName. Ignoring.");
                return;
            }

            var evt = new TelemetryEvent(eventName, properties);
            _buffer.Enqueue(evt);

            onServiceLog?.Invoke($"Event queued: {eventName} (buffer: {_buffer.Count})");
        }

        /// <summary>
        /// Forces an immediate flush of all buffered events. 
        /// Useful for critical events or before application quit.
        /// </summary>
        public async UniTask FlushAsync()
        {
            if (!_isInitialized || _buffer.IsEmpty) return;
            await SendBatchWithRetry();
        }

        private async UniTaskVoid StartFlushLoop()
        {
            if (_isFlushRunning) return;
            _isFlushRunning = true;

            onServiceLog?.Invoke($"Telemetry flush loop started (interval: {_flushInterval}s)");

            while (_isFlushRunning && this != null)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_flushInterval), ignoreTimeScale: true);

                if (!_isFlushRunning || this == null) break;
                if (_buffer.IsEmpty) continue;

                await SendBatchWithRetry();
            }
        }

        private async UniTask SendBatchWithRetry()
        {
            // Proactive token refresh
            if (_session.IsExpiringSoon || _session.IsExpired)
            {
                bool refreshed = await RefreshSessionIfNeeded();
                if (!refreshed)
                {
                    Debug.LogWarning("[ConkistSDK] Could not refresh token. Events remain buffered for next attempt.");
                    return;
                }
                // Invalidate cached QR code since token changed
                InvalidateQRCode();
            }

            var batch = _buffer.Drain();
            if (batch.Count == 0) return;

            int attempt = 0;
            float delay = 1f;

            while (attempt < MaxRetryAttempts)
            {
                attempt++;
                var response = await _apiClient.SendTelemetryBatchAsync(batch, _session.SessionToken);

                if (response.IsSuccess)
                {
                    onServiceLog?.Invoke($"Telemetry batch sent successfully ({batch.Count} events).");
                    return;
                }

                // 401 Unauthorized — invalidate token, re-initialize, retry once
                if (response.IsUnauthorized)
                {
                    Debug.LogWarning("[ConkistSDK] 401 Unauthorized on telemetry send. Refreshing session...");
                    _session.SessionToken = null;

                    var savedPlayerId = PlayerPrefs.GetString("Conkist_PlayerId", null);
                    var refreshResponse = await _apiClient.InitializeSessionAsync(_publicKey, savedPlayerId);
                    if (refreshResponse.IsSuccess && refreshResponse.Data != null)
                    {
                        _session.SessionToken = refreshResponse.Data.SessionToken;
                        _session.ExpiresAt = DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes);
                        UpdateSessionTimes(_session.ExpiresAt);
                        InvalidateQRCode();

                        // Retry the batch with the new token
                        var retryResponse = await _apiClient.SendTelemetryBatchAsync(batch, _session.SessionToken);
                        if (retryResponse.IsSuccess)
                        {
                            onServiceLog?.Invoke($"Telemetry batch sent after token refresh ({batch.Count} events).");
                            return;
                        }
                    }

                    // If we still can't authenticate, re-enqueue and give up this cycle
                    Debug.LogError("[ConkistSDK] Failed to recover from 401. Re-enqueuing events.");
                    _buffer.ReEnqueue(batch);
                    return;
                }

                // 429 / 5xx — retry with exponential backoff + jitter
                if (response.ShouldRetry)
                {
                    if (attempt >= MaxRetryAttempts)
                    {
                        Debug.LogError($"[ConkistSDK] Telemetry send failed after {MaxRetryAttempts} attempts. Re-enqueuing {batch.Count} events.");
                        _buffer.ReEnqueue(batch);
                        return;
                    }

                    float jitter = UnityEngine.Random.Range(0f, 0.5f);
                    float waitTime = delay + jitter;

                    Debug.LogWarning($"[ConkistSDK] Telemetry send failed ({response.StatusCode}). Retry {attempt}/{MaxRetryAttempts} in {waitTime:F1}s");
                    onServiceLog?.Invoke($"Retry {attempt}/{MaxRetryAttempts} in {waitTime:F1}s");

                    await UniTask.Delay(TimeSpan.FromSeconds(waitTime), ignoreTimeScale: true);
                    delay *= 2; // Exponential backoff
                    continue;
                }

                // Other errors (400, etc.) — don't retry, drop the batch
                Debug.LogError($"[ConkistSDK] Telemetry send failed with non-retryable error ({response.StatusCode}): {response.ErrorMessage}");
                onServiceLog?.Invoke($"Batch dropped due to error {response.StatusCode}");
                return;
            }
        }

        // ─────────────────────────────────────────────
        // Unity Editor Helpers
        // ─────────────────────────────────────────────

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Sets the public key for testing.
        /// </summary>
        public void SetPublicKey(string key) => _publicKey = key;

        /// <summary>
        /// Editor-only: Sets the secret key for testing.
        /// </summary>
        public void SetSecretKey(string key) => _secretKey = key;

        /// <summary>
        /// Editor-only: Sets the debug API URL for testing.
        /// </summary>
        public void SetDebugApiUrl(string url) => _debugApiUrl = url;
#endif

        private void OnApplicationQuit()
        {
            // Attempt a final flush before the application quits
            if (_isInitialized && !_buffer.IsEmpty)
            {
                onServiceLog?.Invoke("Application quitting — flushing remaining events...");
                FlushAsync().Forget();
            }
        }
    }
}
