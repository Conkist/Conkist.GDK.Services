using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Component responsible for polling the Conkist API to check if the user 
    /// has linked their account via the web app.
    /// Fires UnityEvents when the link is confirmed or expired.
    /// </summary>
    [AddComponentMenu("Conkist/Network/ConkistLinkPolling")]
    public class ConkistLinkPolling : MonoBehaviour
    {
        [Header("Polling Settings")]
        [Tooltip("How often to check the API for link status (in seconds).")]
        [SerializeField] private float _pollingIntervalSeconds = 3f;
        
        [Tooltip("If true, starts polling automatically as soon as the Connection Code is received.")]
        [SerializeField] private bool _autoStartOnCodeReceived = true;

        [Header("Events")]
        [Tooltip("Fired when the web application confirms the account link.")]
        public UnityEvent OnLinkDetected;
        
        [Tooltip("Fired if the linking code expires before the user links it.")]
        public UnityEvent OnLinkExpired;
        
        [Tooltip("Fired when a polling error occurs.")]
        public UnityEvent<string> OnError;

        private ConkistBackendService _conkistService;
        private CancellationTokenSource _cts;

        /// <summary>
        /// True if the polling task is currently running.
        /// </summary>
        public bool IsPollingActive { get; private set; }

        private void OnEnable()
        {
            if (ConkistBackendService.HasInstance)
            {
                _conkistService = ConkistBackendService.Instance as ConkistBackendService;
                if (_conkistService != null)
                {
                    _conkistService.OnConnectionCodeReceived += HandleCodeReceived;
                    
                    // If we missed the event but the code is already there
                    if (_autoStartOnCodeReceived && !string.IsNullOrEmpty(_conkistService.ConnectionCode))
                    {
                        StartPolling();
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (_conkistService != null)
            {
                _conkistService.OnConnectionCodeReceived -= HandleCodeReceived;
            }
            StopPolling();
        }

        private void HandleCodeReceived(string code)
        {
            if (_autoStartOnCodeReceived)
            {
                StartPolling();
            }
        }

        /// <summary>
        /// Starts the asynchronous polling task.
        /// </summary>
        public void StartPolling()
        {
            if (IsPollingActive) return;

            if (_conkistService == null || string.IsNullOrEmpty(_conkistService.ConnectionToken) || string.IsNullOrEmpty(_conkistService.ConnectionCode))
            {
                Debug.LogWarning("[ConkistLinkPolling] Cannot start polling. Service is not initialized or ConnectionCode is missing.");
                return;
            }

            IsPollingActive = true;
            _cts = new CancellationTokenSource();
            PollTaskAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// Stops the asynchronous polling task.
        /// </summary>
        public void StopPolling()
        {
            IsPollingActive = false;
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async UniTaskVoid PollTaskAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsPollingActive)
            {
                var response = await _conkistService.CheckLinkStatusAsync();

                if (token.IsCancellationRequested) break;

                if (response.IsSuccess)
                {
                    if (response.Data.Status == "confirmed")
                    {
                        StopPolling();
                        OnLinkDetected?.Invoke();
                        break;
                    }
                    else if (response.Data.Status == "expired")
                    {
                        StopPolling();
                        OnLinkExpired?.Invoke();
                        break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ConkistLinkPolling] Error checking link status: {response.ErrorMessage}");
                    OnError?.Invoke(response.ErrorMessage);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(_pollingIntervalSeconds), cancellationToken: token);
            }
        }
    }
}
