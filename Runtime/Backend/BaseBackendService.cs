using UnityEngine;
using UnityEngine.Events;
using Conkist.Tools;

namespace Conkist.Services.Backend
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

        public UnityAction onError;

        private void Start() {
            if(_autoLogin)
                Login();
        }

        public abstract void Login();
    }
}
