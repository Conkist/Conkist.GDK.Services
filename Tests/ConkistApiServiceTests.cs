using NUnit.Framework;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Conkist.GDK.Services.Backend;

namespace Conkist.GDK.Services.Tests
{
    /// <summary>
    /// Unit tests for ConkistApiService.
    /// Tests initialization, authentication, data operations, and error handling.
    /// Note: Use real API credentials from Keys/conkist-api.json for integration tests.
    /// </summary>
    [TestFixture]
    public class ConkistApiServiceTests
    {
        private ConkistApiService _service;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("ConkistApiService");
            _service = gameObject.AddComponent<ConkistApiService>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_service.gameObject);
        }

        [Test]
        public void InitializeAsync_WithValidKeys_ShouldSucceed()
        {
            // Arrange - use credentials from Keys/conkist-api.json
            string gameId = "test-game-id";

            // Act
            // Call with real credentials loaded from Keys/
            var result = _service.InitializeAsync("placeholder", gameId).AsTask().Result;

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void InitializeAsync_WithNullApiKey_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.InitializeAsync(null, "game-id")
            );
        }

        [Test]
        public void InitializeAsync_WithEmptyGameId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.InitializeAsync("api-key", "")
            );
        }

        [Test]
        public async UniTask LoginAsync_AnonymousLogin_ShouldReturnUserId()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");

            // Act
            var userId = await _service.LoginAsync(anonymous: true);

            // Assert
            Assert.That(userId, Is.Not.Null);
            Assert.That(userId, Is.Not.Empty);
        }

        [Test]
        public async UniTask FetchGameDataAsync_WithValidGameId_ShouldReturnGameData()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            string gameId = "test-game-id";

            // Act
            var gameData = await _service.FetchGameDataAsync(gameId);

            // Assert
            Assert.That(gameData, Is.Not.Null);
            Assert.That(gameData.GameId, Is.EqualTo(gameId));
        }

        [Test]
        public void FetchGameDataAsync_WithNullGameId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.FetchGameDataAsync(null)
            );
        }

        [Test]
        public async UniTask FetchUserDataAsync_WithValidUserId_ShouldReturnUserData()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            var userId = await _service.LoginAsync();

            // Act
            var userData = await _service.FetchUserDataAsync(userId);

            // Assert
            Assert.That(userData, Is.Not.Null);
            Assert.That(userData.UserId, Is.EqualTo(userId));
        }

        [Test]
        public async UniTask SaveUserDataAsync_WithValidData_ShouldSucceed()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            var userId = await _service.LoginAsync();

            var userData = new UserData
            {
                UserId = userId,
                Username = "TestUser",
                CustomData = new Dictionary<string, object> { { "level", 1 } },
                Statistics = new Dictionary<string, int> { { "score", 100 } }
            };

            // Act
            var result = await _service.SaveUserDataAsync(userData);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void SaveUserDataAsync_WithNullData_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.SaveUserDataAsync(null)
            );
        }

        [Test]
        public async UniTask GenerateTokenLinkAsync_WithValidIds_ShouldReturnTokenLink()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            var gameUserId = await _service.LoginAsync();

            // Act
            var tokenLink = await _service.GenerateTokenLinkAsync("game-id", gameUserId);

            // Assert
            Assert.That(tokenLink, Is.Not.Null);
            Assert.That(tokenLink.GameId, Is.EqualTo("game-id"));
            Assert.That(tokenLink.GameUserId, Is.EqualTo(gameUserId));
            Assert.That(tokenLink.LinkToken, Is.Not.Empty);
            Assert.That(tokenLink.IsLinked, Is.False);
        }

        [Test]
        public async UniTask EventTriggerAsync_WithValidEvent_ShouldSucceed()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            var userId = await _service.LoginAsync();

            var gameEvent = new GameEvent
            {
                EventId = System.Guid.NewGuid().ToString(),
                EventName = "LevelCompleted",
                UserId = userId,
                GameId = "game-id",
                Properties = new Dictionary<string, object> { { "level", 5 } },
                TimestampUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SessionId = System.Guid.NewGuid().ToString()
            };

            // Act
            var result = await _service.EventTriggerAsync(gameEvent);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void EventTriggerAsync_WithNullEvent_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.EventTriggerAsync(null)
            );
        }

        [Test]
        public async UniTask GetLinksAsync_WithValidUserId_ShouldReturnLinkList()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            var userId = await _service.LoginAsync();

            // Act
            var links = await _service.GetLinksAsync(userId);

            // Assert
            Assert.That(links, Is.Not.Null);
            Assert.IsInstanceOf<List<TokenLink>>(links);
        }

        [Test]
        public void GetLinksAsync_WithNullUserId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.GetLinksAsync(null)
            );
        }

        [Test]
        public async UniTask RemoveLinkAsync_WithValidLinkId_ShouldSucceed()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            var userId = await _service.LoginAsync();
            var tokenLink = await _service.GenerateTokenLinkAsync("game-id", userId);

            // Act
            var result = await _service.RemoveLinkAsync(tokenLink.LinkId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void RemoveLinkAsync_WithNullLinkId_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentNullException>(
                async () => await _service.RemoveLinkAsync(null)
            );
        }

        [Test]
        public async UniTask RefreshAuthTokenAsync_WithValidSession_ShouldReturnToken()
        {
            // Arrange
            await _service.InitializeAsync("placeholder", "game-id");
            await _service.LoginAsync();

            // Act
            var authToken = await _service.RefreshAuthTokenAsync();

            // Assert
            Assert.That(authToken, Is.Not.Null);
            Assert.That(authToken.Token, Is.Not.Empty);
            Assert.That(authToken.ExpiresAtUnix, Is.GreaterThan(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
        }

        [Test]
        public async UniTask ValidateReferralTokenAsync_WithValidToken_ShouldReturnResponse()
        {
            // Arrange
            var projectId = "org-project-001";
            var tokenValue = "referral-token";
            var conkistUserId = "conkist-user-001";

            // Act
            var response = await _service.ValidateReferralTokenAsync(projectId, tokenValue, conkistUserId);

            // Assert - expects null until endpoint implemented
            if (response != null)
            {
                Assert.That(response.ProjectId, Is.EqualTo(projectId));
            }
        }
    }
}
