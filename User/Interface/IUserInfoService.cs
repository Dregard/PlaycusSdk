using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playcus
{
    public interface IUserInfoService
    {
        /// <summary>
        /// Current session is first launch of game?
        /// </summary>
        bool IsFirstLaunch { get; }
        
        /// <summary>
        /// Date then user first launched game
        /// </summary>
        DateTime InstallDate { get; }
        
        /// <summary>
        /// Date of previous launch game
        /// </summary>
        DateTime PreviousLaunchDate { get; }
        
        /// <summary>
        /// Count of unique days then player open game
        /// </summary>
        int PlayingDaysCount { get; }
        
        /// <summary>
        /// Count of all app launches
        /// </summary>
        int SessionCount { get; }
        
        /// <summary>
        /// Link that was passed to app on current session
        /// </summary>
        string DeepLink { get; set; }
        
        /// <summary>
        /// In game Name manual entered by User
        /// </summary>
        string PlayerName { get; set; }
        
        /// <summary>
        /// Gets the user ID
        /// </summary>
        public string UserID { get; }
        
        /// <summary>
        /// Gets whether the user is new
        /// </summary>
        public bool IsNewPlayer { get; }
    }
}