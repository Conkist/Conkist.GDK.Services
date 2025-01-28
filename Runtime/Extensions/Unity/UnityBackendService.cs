using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Cysharp.Threading.Tasks;
using Conkist.GDK.Services.Backend;

namespace Conkist.GDK.Services.Unity
{
    public class UnityBackendService : BaseBackendService
    {
        private const string AccessTokenKey = "AccessToken";
        private bool _isAuthenticated;
        public bool IsAuthenticated => _isAuthenticated;

        public string PlayerID => IsAuthenticated ? UserProfile.PlayerId : null;
        public string AccessToken => PlayerPrefs.GetString(AccessTokenKey, null);

        protected override async void Start()
        {
            await UnityServices.InitializeAsync(); //UNITY BASE INITIALIZATION
            Debug.Log(UnityServices.State);

            SetupEvents();
            
            base.Start();
        }

        public override async UniTask<string> LoginAsync()
        {
            await SignInAnonymouslyAsync();
            return PlayerID;
        }

        private void SetupEvents()
        {

            UnityServices.InitializeFailed += (err) =>
            {
                Debug.LogError(err);
            };
            
            AuthenticationService.Instance.SignedIn += () =>
            {
                _isAuthenticated = true;
                UserProfile = new BackendUserProfile(AuthenticationService.Instance.PlayerId);
                PlayerPrefs.SetString(AccessTokenKey, AuthenticationService.Instance.AccessToken);

                Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");
                Debug.Log($"AccessToken: {AuthenticationService.Instance.AccessToken}");
            };

            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                Debug.LogError(err);
            };

            AuthenticationService.Instance.Expired += () =>
            {
                Debug.LogWarning("Player session could not be refreshed and expired.");
            };
        }

        private async UniTask SignInAnonymouslyAsync()
        {
            // if(!string.IsNullOrEmpty(AccessToken)) return; //Handle signin persistence

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Sign in anonymously succeded!");
                _onLoggedIn?.Invoke();
            }
            catch (AuthenticationException authEx)
            {
                Debug.LogError(authEx);
            }
            catch (RequestFailedException reqEx)
            {
                Debug.LogError(reqEx);
            }
        }
    }
}
