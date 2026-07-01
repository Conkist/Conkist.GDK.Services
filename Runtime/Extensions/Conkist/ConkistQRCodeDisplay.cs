using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// UI Component that displays the connection QR code and the short linking code.
    /// Binds dynamically to ConkistBackendService session initialization events.
    /// Supports target graphics of type RawImage or Image.
    /// Exposes token refresh progress going from 0 to 1 float.
    /// </summary>
    [AddComponentMenu("Conkist/UI/ConkistQRCodeDisplay")]
    public class ConkistQRCodeDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The graphic component (RawImage or Image) where the QR code will be displayed.")]
        [SerializeField] private Graphic _qrGraphic;

        [Tooltip("The text component where the short linking code will be displayed.")]
        [SerializeField] private TMP_Text _codeText;

        [Tooltip("Optional Slider UI component representing the refresh progress.")]
        [SerializeField] private Slider _refreshSlider;

        [Header("QR Settings")]
        [Tooltip("Size of each QR module in pixels.")]
        [SerializeField] private int _pixelsPerModule = 8;

        [Tooltip("Auto-generate and display when the Conkist service initializes.")]
        [SerializeField] private bool _autoDisplayOnInitialize = true;

        [Header("Refresh Progress Events")]
        [Tooltip("Fires with the progress of the current token life (0 to 1, where 1 means it is about to be refreshed/expired).")]
        [SerializeField] private UnityEvent<float> _onRefreshQRCodeProgress;

        /// <summary>
        /// C# Event that fires every frame with the progress of the token lifetime (0 to 1).
        /// </summary>
        public event Action<float> OnRefreshQRCodeProgress;

        private ConkistBackendService _conkistService;
        private bool _isRefreshing = false;

        private void OnEnable()
        {
            // Find and subscribe to ConkistBackendService initialization
            if (ConkistBackendService.HasInstance)
            {
                _conkistService = ConkistBackendService.Instance as ConkistBackendService;
                if (_conkistService != null)
                {
                    _conkistService.OnSessionInitialized += HandleSessionInitialized;
                    _conkistService.OnConnectionCodeReceived += HandleConnectionCodeReceived;

                    // If already initialized and we have a cached QR code or active session, update display immediately
                    if (_conkistService.IsInitialized)
                    {
                        UpdateDisplayAsync().Forget();
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (_conkistService != null)
            {
                _conkistService.OnSessionInitialized -= HandleSessionInitialized;
                _conkistService.OnConnectionCodeReceived -= HandleConnectionCodeReceived;
            }
        }

        private void Update()
        {
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_conkistService != null && _conkistService.IsInitialized && _conkistService.NextRefreshTime != default(DateTime))
            {
                DateTime now = DateTime.UtcNow;
                DateTime last = _conkistService.LastRefreshTime;
                DateTime next = _conkistService.NextRefreshTime;

                double totalDuration = (next - last).TotalSeconds;
                if (totalDuration > 0)
                {
                    double elapsed = (now - last).TotalSeconds;
                    float progress = Mathf.Clamp01((float)(elapsed / totalDuration));

                    _onRefreshQRCodeProgress?.Invoke(progress);
                    OnRefreshQRCodeProgress?.Invoke(progress);

                    if (_refreshSlider != null)
                    {
                        _refreshSlider.value = progress;
                    }

                    // Trigger proactive check/refresh at the end of the countdown
                    if (progress >= 1.0f && !_isRefreshing)
                    {
                        TriggerAutoRefreshAsync().Forget();
                    }
                }
                else
                {
                    ResetProgress();
                }
            }
            else
            {
                ResetProgress();
            }
        }

        private void ResetProgress()
        {
            _onRefreshQRCodeProgress?.Invoke(0f);
            OnRefreshQRCodeProgress?.Invoke(0f);
            if (_refreshSlider != null)
            {
                _refreshSlider.value = 0f;
            }
        }

        private async UniTaskVoid TriggerAutoRefreshAsync()
        {
            _isRefreshing = true;
            try
            {
                // 1. Force refresh session token if expiring
                await _conkistService.CheckAndRefreshSessionAsync();
                
                // 2. Invalidate current QR code/linking code to trigger new generation
                _conkistService.InvalidateQRCode();
                
                // 3. Fetch/Generate the new connection data and QR code
                await UpdateDisplayAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConkistSDK] Failed to auto-refresh session/QR connection: {ex.Message}");
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void HandleSessionInitialized()
        {
            if (_autoDisplayOnInitialize)
            {
                UpdateDisplayAsync().Forget();
            }
        }

        private void HandleConnectionCodeReceived(string code)
        {
            if (_codeText != null)
            {
                _codeText.text = code;
            }
        }

        /// <summary>
        /// Manually triggers fetching of the QR code and linking code from the Conkist service.
        /// </summary>
        public async UniTask UpdateDisplayAsync()
        {
            if (_conkistService == null)
            {
                if (ConkistBackendService.HasInstance)
                {
                    _conkistService = ConkistBackendService.Instance as ConkistBackendService;
                }
            }

            if (_conkistService == null || !_conkistService.IsInitialized)
            {
                Debug.LogWarning("[ConkistSDK] Cannot update QR display: ConkistBackendService is not initialized.");
                return;
            }

            // Generate/retrieve the QR code texture from the backend service
            Texture2D qrTexture = await _conkistService.GetConnectionQRCodeAsync(_pixelsPerModule);
            if (qrTexture != null)
            {
                DisplayQRCode(qrTexture);
            }

            // Display the code
            if (_codeText != null && !string.IsNullOrEmpty(_conkistService.ConnectionCode))
            {
                _codeText.text = _conkistService.ConnectionCode;
            }
        }

        private void DisplayQRCode(Texture2D texture)
        {
            if (_qrGraphic == null) return;

            if (_qrGraphic is RawImage rawImage)
            {
                rawImage.texture = texture;
            }
            else if (_qrGraphic is Image image)
            {
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                image.sprite = sprite;
            }
            else
            {
                Debug.LogWarning("[ConkistSDK] Target Graphic is neither RawImage nor Image. Cannot assign QR texture.");
            }
        }
    }
}
