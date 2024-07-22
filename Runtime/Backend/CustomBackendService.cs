using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Conkist.GDK.Services.Backend
{
    public class CustomBackendService : BaseBackendService
    {
        [Space, Header("Customs")]
        [SerializeField] protected string servicePath;
        [SerializeField] protected string secretKey;

        [SerializeField] EndpointConfiguration loginConfiguration;
        [SerializeField] EndpointConfiguration signupConfiguration;
        [SerializeField] EndpointConfiguration passwordRecoveryConfiguration;
        [SerializeField] EndpointConfiguration deleteConfiguration;

        public void Setup(string path, string secret)
        {
            servicePath = path;
            secretKey = secret;
        }

        public override async UniTask<string> LoginAsync(string user, string password)
        {
            onServiceLog?.Invoke("Login start");
            loginConfiguration.URI = new Uri(servicePath + loginConfiguration.endpoint);
            loginConfiguration.Key = secretKey;
            loginConfiguration.Init();

            try
            {
                loginConfiguration.Assign(loginConfiguration.keyList[0], user);
                loginConfiguration.Assign(loginConfiguration.keyList[1], password);
            }
            catch (ArgumentException arg) { Debug.LogError(arg.Message); }

            using (var www = loginConfiguration.GetRequest())
            {
                await www.SendWebRequest();

                //Request success
                if(www.result == UnityWebRequest.Result.Success)
                {
                    //Check if server returns success
                    var response = www.downloadHandler.text;

                    if(www.responseCode == 200)
                    {
                        onServiceLog?.Invoke("Login success");
                        Debug.LogError("Login success");
                        return www.downloadHandler.text;
                    }
                    if(www.responseCode >= 400)
                    {
                        onServiceLog?.Invoke("Server fail " + www.responseCode);
                        Debug.LogError("Server fail" + www.responseCode);
                        return null;
                    }
                    else
                    {
                        onServiceLog?.Invoke("Login fail " + www.responseCode);
                        Debug.LogError("Login fail" + www.responseCode);
                        return null;
                    }
                }
                else
                {
                    onServiceLog?.Invoke("Request fail " + www.responseCode);
                    Debug.LogError("Request fail" + www.responseCode);
                    return null;
                }
            }
        }

        public override async UniTask SignUpAsync(string user, string password)
        {
            onServiceLog?.Invoke("Signup start");
            signupConfiguration.URI = new Uri(servicePath + loginConfiguration.endpoint);
            signupConfiguration.Key = secretKey;

            signupConfiguration.Assign(loginConfiguration.keyList[0], user);
            signupConfiguration.Assign(loginConfiguration.keyList[1], password);

            await signupConfiguration.GetRequest().SendWebRequest();
            onServiceLog?.Invoke("Signup end");
        }

        public override async UniTask PasswordRecoverAsync(string user)
        {
            onServiceLog?.Invoke("Recovery start");
            passwordRecoveryConfiguration.URI = new Uri(servicePath + loginConfiguration.endpoint);
            passwordRecoveryConfiguration.Key = secretKey;

            passwordRecoveryConfiguration.Assign(loginConfiguration.keyList[0], user);

            await passwordRecoveryConfiguration.GetRequest().SendWebRequest();
            onServiceLog?.Invoke("Recovery end");
        }

        public override async UniTask DeleteAsync(string user, string confirmationCode)
        {
            onServiceLog?.Invoke("Delete start");
            deleteConfiguration.URI = new Uri(servicePath + loginConfiguration.endpoint);
            deleteConfiguration.Key = secretKey;

            deleteConfiguration.Assign(loginConfiguration.keyList[0], user);

            await deleteConfiguration.GetRequest().SendWebRequest();
            onServiceLog?.Invoke("Delete end");
        }

#if UNITY_EDITOR
        public void SetLoginConfig(EndpointConfiguration config) => loginConfiguration = config;
        public void SetSignUpConfig(EndpointConfiguration config) => signupConfiguration = config;
        public void SetResetPassConfig(EndpointConfiguration config) => passwordRecoveryConfiguration = config;
        public void SetDeleteConfig(EndpointConfiguration config) => deleteConfiguration = config;
#endif
    }
}
