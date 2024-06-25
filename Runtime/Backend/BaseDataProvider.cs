using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Conkist.GDK.Services.Backend
{
    /// <summary>
    /// Abstract base class for data providers.
    /// </summary>
    public abstract class BaseDataProvider : Singleton<BaseDataProvider>
    {
        /// <summary>
        /// Dictionary to store cached data.
        /// </summary>
        protected static Dictionary<string, string> _cachedData = new Dictionary<string, string>();

        /// <summary>
        /// Event triggered when data is fetched successfully.
        /// </summary>
        [SerializeField]
        protected UnityEvent _onDataFetched = new UnityEvent();

        /// <summary>
        /// Action invoked when an error occurs during data fetching.
        /// </summary>
        public UnityAction? onError;

        /// <summary>
        /// Fetches the data synchronously.
        /// Must be implemented by derived classes.
        /// </summary>
        public abstract void FetchData();

        /// <summary>
        /// Fetches the data asynchronously.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public virtual async UniTask FetchDataAsync()
        {
            await UniTask.Yield();
        }

        /// <summary>
        /// Invokes the onDataFetched event if it has listeners.
        /// </summary>
        protected void InvokeDataFetched()
        {
            _onDataFetched?.Invoke();
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
