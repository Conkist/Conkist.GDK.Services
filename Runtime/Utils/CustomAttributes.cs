using System;

namespace Conkist.Services
{
    /// <summary>
    /// The idea of this attribute is to control the methods to be used only when a backendservice is authenticated
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public sealed class WhenAuthenticatedAttribute : Attribute { }
}