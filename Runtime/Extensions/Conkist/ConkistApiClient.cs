using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Conkist.GDK.Services.Conkist
{
    /// <summary>
    /// Low-level HTTP transport layer for communicating with the Conkist API.
    /// Handles JSON serialization/deserialization and UnityWebRequest lifecycle.
    /// </summary>
    public class ConkistApiClient
    {
        private readonly string _baseUrl;

        public ConkistApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Calls POST /v1/sdk/initialize with the given public key.
        /// Returns a tuple of (playerId, sessionToken) on success.
        /// </summary>
        /// <returns>
        /// ApiResponse containing the deserialized InitializeResponse or error info.
        /// </returns>
        public async UniTask<ApiResponse<InitializeResponse>> InitializeSessionAsync(string publicKey)
        {
            var requestBody = JsonConvert.SerializeObject(new { publicKey });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/initialize", requestBody))
            {
                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception ex)
                {
                    return ApiResponse<InitializeResponse>.NetworkError(ex.Message);
                }

                return ParseResponse<InitializeResponse>(request);
            }
        }

        /// <summary>
        /// Calls POST /v1/sdk/links to generate a short account linking code.
        /// Authenticated by the project's Secret Key.
        /// </summary>
        public async UniTask<ApiResponse<CreateSdkLinkResponse>> CreateLinkRequestAsync(string userId, string secretKey)
        {
            var requestBody = JsonConvert.SerializeObject(new { userId });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/links", requestBody))
            {
                request.SetRequestHeader("Authorization", $"Bearer {secretKey}");

                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception ex)
                {
                    return ApiResponse<CreateSdkLinkResponse>.NetworkError(ex.Message);
                }

                return ParseResponse<CreateSdkLinkResponse>(request);
            }
        }

        /// <summary>
        /// Calls POST /v1/sdk/telemetry with a batch of events, authenticated by Bearer token.
        /// </summary>
        /// <returns>
        /// ApiResponse indicating success (202) or failure with status code.
        /// </returns>
        public async UniTask<ApiResponse<string>> SendTelemetryBatchAsync(List<TelemetryEvent> events, string sessionToken)
        {
            var requestBody = JsonConvert.SerializeObject(new { events }, new JsonSerializerSettings
            {
                // Ensure properties Dictionary<string, object> is serialized correctly
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/telemetry", requestBody))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception ex)
                {
                    return ApiResponse<string>.NetworkError(ex.Message);
                }

                if (request.responseCode == 202)
                {
                    return ApiResponse<string>.Success(request.downloadHandler.text, (int)request.responseCode);
                }

                return ApiResponse<string>.Failure(
                    (int)request.responseCode,
                    request.downloadHandler?.text ?? request.error
                );
            }
        }

        private UnityWebRequest CreatePostRequest(string url, string jsonBody)
        {
            var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private ApiResponse<T> ParseResponse<T>(UnityWebRequest request) where T : class
        {
            int statusCode = (int)request.responseCode;

            if (request.result == UnityWebRequest.Result.Success && statusCode == 200)
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
                    return ApiResponse<T>.Success(data, statusCode);
                }
                catch (Exception ex)
                {
                    return ApiResponse<T>.Failure(statusCode, $"JSON parse error: {ex.Message}");
                }
            }

            return ApiResponse<T>.Failure(statusCode, request.downloadHandler?.text ?? request.error);
        }
    }

    /// <summary>
    /// Generic API response wrapper with status code and error information.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; private set; }
        public int StatusCode { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool IsNetworkError { get; private set; }

        public bool IsUnauthorized => StatusCode == 401;
        public bool IsRateLimited => StatusCode == 429;
        public bool IsServerError => StatusCode >= 500;
        public bool ShouldRetry => IsRateLimited || IsServerError || IsNetworkError;

        public static ApiResponse<T> Success(T data, int statusCode)
        {
            return new ApiResponse<T> { IsSuccess = true, Data = data, StatusCode = statusCode };
        }

        public static ApiResponse<T> Failure(int statusCode, string errorMessage)
        {
            return new ApiResponse<T> { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
        }

        public static ApiResponse<T> NetworkError(string message)
        {
            return new ApiResponse<T> { IsSuccess = false, IsNetworkError = true, StatusCode = 0, ErrorMessage = message };
        }
    }

    /// <summary>
    /// Response model for the /v1/sdk/initialize endpoint.
    /// </summary>
    [Serializable]
    public class InitializeResponse
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("sessionToken")]
        public string SessionToken { get; set; }
    }

    [Serializable]
    public class CreateSdkLinkResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("deeplink")]
        public string Deeplink { get; set; }
    }
}
