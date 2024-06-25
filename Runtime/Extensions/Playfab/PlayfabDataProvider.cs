using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using Conkist.GDK.Services.Backend;

namespace Conkist.GDK.Services.Playfab
{
    /// <summary>
    /// Data provider implementation using PlayFab.
    /// </summary>
    public class PlayfabDataProvider : BaseDataProvider
    {
        /// <summary>
        /// Fetches the data synchronously from PlayFab.
        /// </summary>
        public override void FetchData()
        {
            _cachedData.Clear();

            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnPlayfabDataFetched, OnPlayfabError);
        }

        /// <summary>
        /// Fetches the data asynchronously from PlayFab.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public override async UniTask FetchDataAsync()
        {
            FetchData();
            await UniTask.WaitUntil(() => _cachedData != null && _cachedData.Count > 0);
        }

        /// <summary>
        /// Callback method when PlayFab data is fetched successfully.
        /// </summary>
        /// <param name="result">The result from PlayFab containing the data.</param>
        private void OnPlayfabDataFetched(GetTitleDataResult result)
        {
            _cachedData = result.Data;
            InvokeDataFetched();
        }

        /// <summary>
        /// Callback method when PlayFab data fetch fails.
        /// </summary>
        /// <param name="error">The error information from PlayFab.</param>
        private void OnPlayfabError(PlayFabError error)
        {
            Debug.LogError($"PlayFabClient failed to fetch TitleData: {error.ErrorMessage}");
            InvokeError(new System.Exception(error.ErrorMessage));
        }
    }
}