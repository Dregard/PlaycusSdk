using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus.Network
{
    public interface INetworkManager
    {
        bool CheckNetworkConnection(bool noNetworkActions = true);
    }
}
