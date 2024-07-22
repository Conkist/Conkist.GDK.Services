
using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Conkist.GDK.Services
{

    public enum RequestMethod
    {
        GET,POST,HEAD
    }

    [Serializable]
    public class EndpointConfiguration
    {
        public RequestMethod method;
        public string endpoint;
        public List<string> keyList;

        private List<KeyValuePair<string, string>> form = new List<KeyValuePair<string, string>>();

        public Uri URI { get; internal set; }
        public string Key { get; internal set; }

        public EndpointConfiguration(RequestMethod method, string endpoint, List<string> keyList = null)
        {
            this.method = method;
            this.endpoint = endpoint;
            this.keyList = keyList;
        }

        public void Init()
        {
            form = new List<KeyValuePair<string, string>>();
        }

        //Configurations
        public EndpointConfiguration Assign(string data, string paramKey)
        {
            if (string.IsNullOrEmpty(data)) throw new ArgumentNullException(nameof(data));

            if(!string.IsNullOrEmpty(paramKey) && keyList != null && keyList.Contains(data))
            {
                form.Add(new KeyValuePair<string,string>(paramKey, data));
            }
            else
            {
                throw new ArgumentException("Parameter Key is not in the Endpoint Configuration", nameof(paramKey));
            }
            
            return this;
        }

        public UnityWebRequest GetRequest()
        {
            UnityWebRequest request = null;
            switch(method)
            {
                case RequestMethod.GET: request = UnityWebRequest.Get(URI + endpoint); break;
                case RequestMethod.POST: request = UnityWebRequest.Post(URI + endpoint, new Dictionary<string, string>(form)); break;
                case RequestMethod.HEAD: request = UnityWebRequest.Head(URI + endpoint); break;
            }
            if(request == null) return request;

            request.SetRequestHeader("key", Key);

            return request;
        }
    }
}