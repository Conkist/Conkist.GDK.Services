namespace Conkist.Services
{
    /// <summary>
    /// This interface injects common functions and properties of services that require some sort of user authentication
    /// </summary>
    public interface IAuthenticate
    {
        void Login();
    }
}
