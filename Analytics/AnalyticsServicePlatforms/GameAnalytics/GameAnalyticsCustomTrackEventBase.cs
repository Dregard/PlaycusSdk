using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus.Analytics
{
    public abstract class GameAnalyticsCustomTrackEventBase : MonoBehaviour
    {
        public abstract bool SendEventIfItCustom(AnalyticsEvent analyticsEvent);
    }
}
