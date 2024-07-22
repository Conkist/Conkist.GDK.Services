using Cysharp.Threading.Tasks;

namespace Conkist.GDK.Services
{
    /// <summary>
    /// This interface injects common functions and properties of services that require some sort of user authentication
    /// </summary>
    public interface IAuthenticate
    {
        /// <summary>
        /// Sends a blank login request that might return some value
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation with serialized json data from server</returns>
        UniTask<string> LoginAsync();
    }
}
