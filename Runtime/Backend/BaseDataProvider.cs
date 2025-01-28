using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Conkist.GDK.Services.Backend
{
    /// <summary>
    /// Abstract base class for data providers.
    /// </summary>
    public abstract class BaseDataProvider : Singleton<BaseDataProvider>
    {
        /// <summary>
        /// Dictionary to store app cached data.
        /// </summary>
        protected static Dictionary<string, string> _cachedAppData = new Dictionary<string, string>();
        public static Dictionary<string,string> CachedAppData => _cachedAppData;
    
        /// <summary>
        /// Dictionary to store player cached data.
        /// </summary>
        protected static Dictionary<string, string> _cachedPlayerData = new Dictionary<string, string>();
        public static Dictionary<string,string> CachedPlayerData => _cachedPlayerData;

        /// <summary>
        /// Event triggered when data is fetched successfully.
        /// </summary>
        [SerializeField]
        protected UnityEvent _onAppDataFetched = new UnityEvent();

        [SerializeField]
        protected UnityEvent _onPlayerDataFetched = new UnityEvent();

        /// <summary>
        /// Action invoked when an error occurs during data fetching.
        /// </summary>
        public UnityAction? onError;

        /// <summary>
        /// Fetches the app data synchronously.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract void FetchAppData();

        /// <summary>
        /// Fetches the app data asynchronously.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public virtual async UniTask FetchAppDataAsync()
        {
            await UniTask.Yield();
        }

        /// <summary>
        /// Fetches the player data synchronously.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract void FetchPlayerData();

        /// <summary>
        /// Fetches the player data asynchronously.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public virtual async UniTask FetchPlayerDataAsync()
        {
            await UniTask.Yield();
        }

        /// <summary>
        /// Invokes the onAppDataFetched event if it has listeners.
        /// </summary>
        protected void InvokeAppDataFetched()
        {
            var json = JsonConvert.SerializeObject(_cachedAppData, Formatting.Indented);
            Debug.Log("Fetched data: " + json);
            _onAppDataFetched?.Invoke();
        }

        /// <summary>
        /// Invokes the onPlayerDataFetched event if it has listeners.
        /// </summary>
        protected void InvokePlayerDataFetched()
        {
            var json = JsonConvert.SerializeObject(_cachedPlayerData, Formatting.Indented);
            Debug.Log("Fetched data: " + json);
            _onPlayerDataFetched?.Invoke();
        }

        /// <summary>
        /// Invokes the onError action if it has listeners.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        protected void InvokeError(System.Exception exception)
        {
            onError?.Invoke();
            Debug.LogError($"Data fetching error: {exception.Message}");
        }
    }
}
