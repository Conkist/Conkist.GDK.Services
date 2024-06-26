using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine.Events;
using Conkist.GDK.Services.Backend;
using Cysharp.Threading.Tasks;

namespace Conkist.GDK.Services.Playfab
{
    /// <summary>
    /// Handles PlayFab backend operations like login and profile management.
    /// </summary>
    public class PlayfabBackendService : BaseBackendService
    {
        /// <summary>
        /// Singleton instance of PlayfabBackendService.
        /// </summary>
        public static new PlayfabBackendService Instance => (PlayfabBackendService)_instance;

        /// <summary>
        /// Indicates if the profile data is synchronized with the server.
        /// </summary>
        public bool InSync { get; protected set; }

        /// <summary>
        /// Event triggered when the profile is successfully loaded.
        /// </summary>
        [SerializeField] protected UnityEvent _onProfileLoaded;

        /// <summary>
        /// Asynchronously logs in the user using PlayFab with a custom ID.
        /// </summary>
        public async override UniTask LoginAsync()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        /// <summary>
        /// Callback method executed upon successful login.
        /// Initializes the user profile and updates it.
        /// </summary>
        /// <param name="result">The result from the login request.</param>
        private void OnLoginSuccess(LoginResult result)
        {
            Debug.Log(result.NewlyCreated ? $"[{GetType().Name}] Account Created!" : $"[{GetType().Name}] Logged In!", this);
            Debug.Log($"[{GetType().Name}] PlayfabId: {result.PlayFabId}", this);

            // Initialize user profile with PlayFabId.
            UserProfile = new BackendUserProfile(result.PlayFabId);

            // Update the player profile with additional information.
            UpdatePlayerProfile();

            // Trigger the logged-in event.
            _onLoggedIn?.Invoke();
        }

        /// <summary>
        /// Sends a request to update the player's profile.
        /// </summary>
        private void UpdatePlayerProfile()
        {
            var request = new GetPlayerProfileRequest
            {
                PlayFabId = UserProfile.PlayerId,
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowDisplayName = true,
                    ShowCreated = true,
                    ShowLastLogin = true
                }
            };

            PlayFabClientAPI.GetPlayerProfile(request, OnProfileLoaded, OnError);
        }

        /// <summary>
        /// Callback method executed upon successfully loading the player's profile.
        /// Updates the user profile data and triggers the profile loaded event.
        /// </summary>
        /// <param name="result">The result from the profile request.</param>
        private void OnProfileLoaded(GetPlayerProfileResult result)
        {
            UserProfile.Username = result.PlayerProfile.DisplayName;
            UserProfile.CreateDate = result.PlayerProfile.Created;
            UserProfile.LastLoginDate = result.PlayerProfile.LastLogin;
            UserProfile.LastUpdateDate = DateTime.Now;
            UserProfile.ProfileLoaded = true;

            // Trigger the profile loaded event.
            _onProfileLoaded?.Invoke();
        }

        /// <summary>
        /// Sets the username for the user.
        /// </summary>
        /// <param name="text">The desired username.</param>
        /// <param name="onUpdate">Callback executed when the username is successfully updated.</param>
        /// <param name="onNotAvailable">Callback executed when the desired username is not available.</param>
        public void SetUsername(string text, UnityAction<string> onUpdate, UnityAction onNotAvailable)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                onNotAvailable?.Invoke();
                return;
            }

            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = text
            };

            PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            (result) =>
            {
                UserProfile.Username = result.DisplayName;
                onUpdate(result.DisplayName);
            },
            (error) =>
            {
                if (error.Error != PlayFabErrorCode.NameNotAvailable) OnError(error);
                else onNotAvailable?.Invoke();
            });
        }

        /// <summary>
        /// Callback method for handling PlayFab errors.
        /// Logs warnings and triggers the error event.
        /// </summary>
        /// <param name="error">The error information from PlayFab.</param>
        private void OnError(PlayFabError error)
        {
            Debug.LogWarning(error.GenerateErrorReport());

            // Handle specific error codes.
            switch (error.Error)
            {
                case PlayFabErrorCode.ProfileDoesNotExist:
                    {
                        if (UserProfile != null) UserProfile.ProfileLoaded = true;
                    }
                    break;
            }

            // Trigger the error event.
            onError?.Invoke();
        }
    }
}