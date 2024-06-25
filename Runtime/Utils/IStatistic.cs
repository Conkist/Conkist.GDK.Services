using Cysharp.Threading.Tasks;

namespace Conkist.GDK.Services
{
    /// <summary>
    /// Interface representing a statistic that can be sent.
    /// </summary>
    public interface IStatistic
    {
        /// <summary>
        /// Sends the statistic data asynchronously.
        /// </summary>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        UniTask SendAsync();
    }
}