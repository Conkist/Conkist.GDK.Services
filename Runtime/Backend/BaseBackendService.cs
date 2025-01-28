using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Conkist.GDK.Services.Backend
{
    /// <summary>
    /// A game, for a bunch of reasons, must have only one Backend Service
    /// </para>A place where all data from the game and player is stored, with a cloud service to run functions to manage this data and control remotelly it
    /// </summary>
    public abstract class BaseBackendService : Singleton<BaseBackendService>, IAuthenticate
    {
        public static BackendUserProfile UserProfile { get; protected set; }

        [SerializeField] protected bool _autoLogin = true;
        [SerializeField] protected UnityEvent _onLoggedIn;

        public UnityAction<string> onServiceLog;
        public UnityAction onError;

        protected virtual void Start() {
            if(_autoLogin)
                LoginAsync().Forget();
        }

        public async virtual UniTask<string> LoginAsync() { await UniTask.Yield(); return null; }
        public async virtual UniTask<string> LoginAsync(string user, string password) { await UniTask.Yield(); return null; }
        public async virtual UniTask SignUpAsync(string user, string password) { await UniTask.Yield(); }
        public async virtual UniTask PasswordRecoverAsync(string user) { await UniTask.Yield(); }
        public async virtual UniTask DeleteAsync(string user, string confirmationCode) { await UniTask.Yield(); }
    }
}
