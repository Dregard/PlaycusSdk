using System;
using Cysharp.Threading.Tasks;
using Playcus.Utils;

namespace Playcus.Saves
{
    public interface ISaveService: IService
    {
        [Obsolete("Use RegisterAndLoadAsync instead",true)]
        void RegisterAndLoad(string uniqueKey, object saveVO);
        UniTask RegisterAndLoadAsync(string uniqueKey, object saveVO);
        void Save(bool forceSaveNow = false);
        UniTask DeleteSaveData();

        // LEGACY METHODS for OLD projects
        [Obsolete("Deprecated")]
        JSONNode LoadSave(string saveId);

        [Obsolete("Deprecated")]
        void Register(ISaveable saveable);

        [Obsolete("Deprecated")]
        string LoadKeyAsJson(ISaveable saveable, string key);

        [Obsolete("Deprecated")]
        string SaveKeyToJson(object obj);
    }
}