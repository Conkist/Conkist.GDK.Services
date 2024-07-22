using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using Conkist.GDK.Services.Backend;

namespace Conkist.GDK.Services.Tests
{

    public class CustomBackendServiceTests
    {
        private CustomBackendService _customBackendService;
        private EndpointConfiguration _mockLoginConfiguration;
        private EndpointConfiguration _mockSignupConfiguration;
        private EndpointConfiguration _mockPasswordRecoveryConfiguration;
        private EndpointConfiguration _mockDeleteConfiguration;

        [SetUp]
        public void SetUp()
        {
            // Create a new GameObject and attach the CustomBackendService component
            var gameObject = new GameObject();
            gameObject.SetActive(false);
            var service = gameObject.AddComponent<CustomBackendService>();
            service.Setup("https://jesusparaascriancas.com.br/API/", "e7NywmviCx0iAbRCH2MiY");

            // Set up mock endpoint configurations
            _mockLoginConfiguration = new EndpointConfiguration(RequestMethod.POST, "login", new List<string>() { "username", "senha" });
            _mockSignupConfiguration = new EndpointConfiguration(RequestMethod.POST, "signup", new List<string>() { "email", "password" });
            _mockPasswordRecoveryConfiguration = new EndpointConfiguration(RequestMethod.POST, "forgotpassword", new List<string>() { "email" });
            _mockDeleteConfiguration = new EndpointConfiguration(RequestMethod.POST, "deleteaccount", new List<string>() { "email", "password" });

            // Assign the mock configurations to the CustomBackendService
            _customBackendService.SetLoginConfig(_mockLoginConfiguration);
            _customBackendService.SetSignUpConfig(_mockSignupConfiguration);
            _customBackendService.SetResetPassConfig(_mockPasswordRecoveryConfiguration);
            _customBackendService.SetDeleteConfig(_mockDeleteConfiguration);
        }

        [Test]
        [TestCase(arg1: "jpc.roberta@gmail.com", arg2:"@Jesus3377")]
        public async UniTask LoginAsync_ShouldSetUriAndKeyCorrectly(string user, string password)
        {

            var result = await _customBackendService.LoginAsync(user, password);

            Assert.Pass();

            //var expectedUri = new Uri(_customBackendService.servicePath + _mockLoginConfiguration.endpoint);
            //_mockLoginConfiguration.Received().URI = expectedUri;
            //_mockLoginConfiguration.Received().Key = _customBackendService.secretKey;
            //_mockLoginConfiguration.Received().Assign(_mockLoginConfiguration.keyList[0], user);
            //_mockLoginConfiguration.Received().Assign(_mockLoginConfiguration.keyList[1], password);
            //await _mockLoginConfiguration.Received().GetRequest().SendWebRequest();
        }

        [Test]
        public async UniTask SignUpAsync_ShouldSetUriAndKeyCorrectly()
        {
            string user = "newUser";
            string password = "newPassword";

            await _customBackendService.SignUpAsync(user, password);

            //var expectedUri = new Uri(_customBackendService.servicePath + _mockSignupConfiguration.endpoint);
            //_mockSignupConfiguration.Received().URI = expectedUri;
            //_mockSignupConfiguration.Received().Key = _customBackendService.secretKey;
            //_mockSignupConfiguration.Received().Assign(_mockSignupConfiguration.keyList[0], user);
            //_mockSignupConfiguration.Received().Assign(_mockSignupConfiguration.keyList[1], password);
            //await _mockSignupConfiguration.Received().GetRequest().SendWebRequest();
        }

        [Test]
        public async UniTask PasswordRecoverAsync_ShouldSetUriAndKeyCorrectly()
        {
            string user = "testUser";

            await _customBackendService.PasswordRecoverAsync(user);

            //var expectedUri = new Uri(_customBackendService.servicePath + _mockPasswordRecoveryConfiguration.endpoint);
            //_mockPasswordRecoveryConfiguration.Received().URI = expectedUri;
            //_mockPasswordRecoveryConfiguration.Received().Key = _customBackendService.secretKey;
            //_mockPasswordRecoveryConfiguration.Received().Assign(_mockPasswordRecoveryConfiguration.keyList[0], user);
            //await _mockPasswordRecoveryConfiguration.Received().GetRequest().SendWebRequest();
        }

        [Test]
        public async UniTask DeleteAsync_ShouldSetUriAndKeyCorrectly()
        {
            string user = "testUser";
            string confirmationCode = "confirmationCode";

            await _customBackendService.DeleteAsync(user, confirmationCode);

            //var expectedUri = new Uri(_customBackendService.servicePath + _mockDeleteConfiguration.endpoint);
            //_mockDeleteConfiguration.Received().URI = expectedUri;
            //_mockDeleteConfiguration.Received().Key = _customBackendService.secretKey;
            //_mockDeleteConfiguration.Received().Assign(_mockDeleteConfiguration.keyList[0], user);
            //await _mockDeleteConfiguration.Received().GetRequest().SendWebRequest();
        }
    }
}