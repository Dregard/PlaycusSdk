using System;

namespace Playcus
{
    public class PlaycusInitializationException : Exception
    {
        public PlaycusInitializationException(string serviceName, string error) : base($"{serviceName}: {error}")
        {
        }
    }
}