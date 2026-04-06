using System;
using System.Collections.Generic;

namespace Playcus
{
    [Serializable]
    public class UserInfoSaveVO
    {
        public string InstallDate;
        public string PreviousLaunchDate;
        public int SessionCount;
        public int PlayingDaysCount;
        public string Name;
    }
    
}
