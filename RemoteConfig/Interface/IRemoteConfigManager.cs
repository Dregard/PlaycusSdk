using System;

namespace Playcus
{
    public interface IRemoteConfigManager : IService
    {
        event Action ConfigUpdated;
        string GetValue(string key);
        void UpdateRemoteConfig();
    }
}