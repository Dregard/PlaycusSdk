using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus.Saves
{
    /// <summary>
    /// Can be saved or loaded by SaveManager
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique id of data container in SaveManager (ISaveable)
        /// </summary>
        string SaveId { get; }

        /// <summary>
        /// Save data to SaveManager (ISaveable)
        /// </summary>
        Dictionary<string, object> Save();
    }
}
