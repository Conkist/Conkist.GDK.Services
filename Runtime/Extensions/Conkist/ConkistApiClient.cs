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
        public async UniTask<ApiResponse<InitializeResponse>> InitializeSessionAsync(string publicKey, string playerId = null, string playerSecret = null)
        {
            // No playerId: the API creates a new player and returns its secret once.
            // With playerId: the secret is required.
            var requestBody = JsonConvert.SerializeObject(new { publicKey, playerId, playerSecret });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/initialize", requestBody))
            {
                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<InitializeResponse>.NetworkError(sendError.Message);

                return ParseResponse<InitializeResponse>(request);
            }
        }

        /// <summary>
        /// Calls POST /v1/sdk/links to generate a short account linking code.
        /// Authenticated by the session token (JWT).
        /// </summary>
        public async UniTask<ApiResponse<CreateSdkLinkResponse>> CreateLinkRequestAsync(string sessionToken)
        {
            var requestBody = JsonConvert.SerializeObject(new { });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/links", requestBody))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<CreateSdkLinkResponse>.NetworkError(sendError.Message);

                return ParseResponse<CreateSdkLinkResponse>(request);
            }
        }

        /// <summary>
        /// Calls GET /v1/sdk/links/{id}/status to check the linking status.
        /// Authenticated by the session token (JWT).
        /// </summary>
        public async UniTask<ApiResponse<GetLinkStatusResponse>> GetLinkStatusAsync(long linkId, string sessionToken)
        {
            using (var request = UnityWebRequest.Get($"{_baseUrl}/v1/sdk/links/{linkId}/status"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<GetLinkStatusResponse>.NetworkError(sendError.Message);

                return ParseResponse<GetLinkStatusResponse>(request);
            }
        }

        /// <summary>
        /// Calls POST /v1/sdk/links/redeem: the player types a code that a Conkist account generated in the app.
        /// A wrong code is a 400; too many wrong codes is a 429 (see <see cref="ApiResponse{T}.IsRateLimited"/>).
        /// </summary>
        public async UniTask<ApiResponse<RedeemLinkResponse>> RedeemLinkAsync(string code, string sessionToken)
        {
            var requestBody = JsonConvert.SerializeObject(new { code });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/links/redeem", requestBody))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<RedeemLinkResponse>.NetworkError(sendError.Message);

                return ParseResponse<RedeemLinkResponse>(request);
            }
        }

        /// <summary>
        /// Calls POST /v1/sdk/codes/redeem: one door for any code typed or scanned in the game.
        /// An 8-digit account code confirms a connection (Kind = "link"); a room code joins a room (Kind = "room").
        /// A wrong code is a 400 (same message for both kinds); 5 wrong codes lock the player (429).
        /// </summary>
        public async UniTask<ApiResponse<RedeemCodeResponse>> RedeemCodeAsync(string code, string sessionToken)
        {
            var requestBody = JsonConvert.SerializeObject(new { code });

            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/codes/redeem", requestBody))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<RedeemCodeResponse>.NetworkError(sendError.Message);

                return ParseResponse<RedeemCodeResponse>(request);
            }
        }

        /// <summary>Calls GET /v1/sdk/rooms: the rooms this player is in.</summary>
        public async UniTask<ApiResponse<List<SdkRoom>>> GetRoomsAsync(string sessionToken)
        {
            using (var request = UnityWebRequest.Get($"{_baseUrl}/v1/sdk/rooms"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<List<SdkRoom>>.NetworkError(sendError.Message);

                return ParseResponse<List<SdkRoom>>(request);
            }
        }

        /// <summary>Calls POST /v1/sdk/rooms/{id}/leave: the player leaves a room (204 on success).</summary>
        public async UniTask<ApiResponse<string>> LeaveRoomAsync(long roomId, string sessionToken)
        {
            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/rooms/{roomId}/leave", "{}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<string>.NetworkError(sendError.Message);

                if (request.responseCode == 204 || request.responseCode == 200)
                {
                    return ApiResponse<string>.Success(string.Empty, (int)request.responseCode);
                }

                return ApiResponse<string>.Failure((int)request.responseCode, request.downloadHandler?.text ?? request.error);
            }
        }

        /// <summary>Calls GET /v1/sdk/links: the confirmed links of this player.</summary>
        public async UniTask<ApiResponse<List<PlayerLink>>> GetLinksAsync(string sessionToken)
        {
            using (var request = UnityWebRequest.Get($"{_baseUrl}/v1/sdk/links"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<List<PlayerLink>>.NetworkError(sendError.Message);

                return ParseResponse<List<PlayerLink>>(request);
            }
        }

        /// <summary>Calls POST /v1/sdk/links/{id}/revoke: the player cancels a link (204 on success).</summary>
        public async UniTask<ApiResponse<string>> RevokeLinkAsync(long linkId, string sessionToken)
        {
            using (var request = CreatePostRequest($"{_baseUrl}/v1/sdk/links/{linkId}/revoke", "{}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {sessionToken}");

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<string>.NetworkError(sendError.Message);

                if (request.responseCode == 204 || request.responseCode == 200)
                {
                    return ApiResponse<string>.Success(string.Empty, (int)request.responseCode);
                }

                return ApiResponse<string>.Failure((int)request.responseCode, request.downloadHandler?.text ?? request.error);
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

                var sendError = await SendAsync(request);
                if (sendError != null) return ApiResponse<string>.NetworkError(sendError.Message);

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

        /// <summary>
        /// UniTask throws on any HTTP error. A reply with a status code is not a network failure: it carries a message
        /// (for example 401 "Invalid player credentials." or 429 with a wait time), and ParseResponse reads it from the request.
        /// Only a real failure to reach the server comes back as an exception.
        /// </summary>
        private static async UniTask<Exception> SendAsync(UnityWebRequest request)
        {
            try
            {
                await request.SendWebRequest();
                return null;
            }
            catch (UnityWebRequestException ex) when (ex.ResponseCode > 0)
            {
                return null;
            }
            catch (Exception ex)
            {
                return ex;
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

        /// <summary>Only present when the player was just created. Save it: the API keeps only a hash.</summary>
        [JsonProperty("playerSecret")]
        public string PlayerSecret { get; set; }

        [JsonProperty("isNew")]
        public bool IsNew { get; set; }

        [JsonProperty("sessionToken")]
        public string SessionToken { get; set; }

        [JsonProperty("expiresIn")]
        public int ExpiresIn { get; set; }
    }

    [Serializable]
    public class CreateSdkLinkResponse
    {
        [JsonProperty("linkId")]
        public long LinkId { get; set; }

        /// <summary>8 digits.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("expiresAt")]
        public DateTime ExpiresAt { get; set; }

        [JsonProperty("deeplink")]
        public string Deeplink { get; set; }
    }

    /// <summary>Link states: pending, confirmed, revoked, expired.</summary>
    public static class LinkStatusNames
    {
        public const string Pending = "pending";
        public const string Confirmed = "confirmed";
        public const string Revoked = "revoked";
        public const string Expired = "expired";
    }

    [Serializable]
    public class GetLinkStatusResponse
    {
        [JsonProperty("linkId")]
        public long LinkId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>"player" or "account": who cancelled the link.</summary>
        [JsonProperty("revokedBy")]
        public string RevokedBy { get; set; }
    }

    [Serializable]
    public class RedeemLinkResponse
    {
        [JsonProperty("linkId")]
        public long LinkId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    /// <summary>Which kind of code the server recognised.</summary>
    public static class CodeKindNames
    {
        public const string Link = "link";
        public const string Room = "room";
    }

    /// <summary>Answer of the unified code door. Kind tells which fields are filled.</summary>
    [Serializable]
    public class RedeemCodeResponse
    {
        /// <summary>"link" (account connection) or "room".</summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary>Kind = link: the connection that was confirmed.</summary>
        [JsonProperty("linkId")]
        public long? LinkId { get; set; }

        /// <summary>Kind = link: "confirmed".</summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>Kind = room: the room the player joined.</summary>
        [JsonProperty("room")]
        public SdkRoom Room { get; set; }

        public bool IsLink => Kind == CodeKindNames.Link;
        public bool IsRoom => Kind == CodeKindNames.Room;
    }

    /// <summary>A class room the player is in. The game never gets the teacher's or the other students' data.</summary>
    [Serializable]
    public class SdkRoom
    {
        [JsonProperty("roomId")]
        public long RoomId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("organization")]
        public string Organization { get; set; }

        [JsonProperty("joinedAt")]
        public DateTime JoinedAt { get; set; }
    }

    /// <summary>A confirmed link of this player. Never carries the account e-mail.</summary>
    [Serializable]
    public class PlayerLink
    {
        [JsonProperty("linkId")]
        public long LinkId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("alias")]
        public string Alias { get; set; }

        [JsonProperty("confirmedAt")]
        public DateTime? ConfirmedAt { get; set; }
    }
}
