using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using Conkist.Services.Backend;
using System;
using UnityEngine.Events;

namespace Conkist.Services.Playfab
{
    public class PlayfabBackendService : BaseBackendService
    {
        public static new PlayfabBackendService Instance => (PlayfabBackendService) _instance;

        public bool InSync { get; protected set; }
        
        [SerializeField] protected UnityEvent _onProfileLoaded;

        public override void Login()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        private void OnLoginSuccess(LoginResult result)
        {
            Debug.Log(result.NewlyCreated ? $"[{GetType().Name}] " + "Account Created!" : $"[{GetType().Name}] " + "Logged In!", this);
            Debug.Log($"[{GetType().Name}] " + "PlayfabId: " + result.PlayFabId, this);
            UserProfile = new BackendUserProfile(result.PlayFabId);
            UpdatePlayerProfile();
            _onLoggedIn?.Invoke();
        }

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

        private void OnProfileLoaded(GetPlayerProfileResult result)
        {
            UserProfile.Username = result.PlayerProfile.DisplayName;

            UserProfile.CreateDate = result.PlayerProfile.Created;
            UserProfile.LastLoginDate = result.PlayerProfile.LastLogin;
            UserProfile.LastUpdateDate = DateTime.Now;

            UserProfile.ProfileLoaded = true;
            _onProfileLoaded?.Invoke();
        }

        public void SetUsername(string text, UnityAction<string> onUpdate, UnityAction onNotAvailable)
        {
            if(string.IsNullOrWhiteSpace(text))
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
                if(error.Error != PlayFabErrorCode.NameNotAvailable) OnError(error);
                else onNotAvailable?.Invoke();
            });
        }

        private void OnError(PlayFabError error)
        {
            Debug.LogWarning(error.GenerateErrorReport());
            switch (error.Error)
            {
                case PlayFabErrorCode.ProfileDoesNotExist:
                {
                    if(UserProfile != null) UserProfile.ProfileLoaded = true;  
                }break;
            }
            onError?.Invoke();
        }
    }
}
