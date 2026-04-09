namespace Playcus
{
    /// SETUP INSTRUCTION
    /// 1. Copy this file in project repository.
    /// 2. Uncomment class body.
    /// 3. Fill enums with project's config values
    /// 4. Do not change enum names!
    /// 5. Do not change predefined enum elements!
    /// 6. Enum values will be used as converted to strings or as is with different submodule-core systems


    // REMOVE this line to begin your journey


    /// <summary>
    /// Place where can be showed ads or promo. Place there from iap can be purchased. etc
    /// </summary>
    public enum PLACE
    {
        Loading = 0,
        NewLoading = 1,
        Expand = 2,
        Progress = 3,
        Shop = 4,
        Promo = 5,
        PuzzleCollection = 6,
        DailyBonus = 7,
        OpenReward = 8,
        // Core reserved values 0-99. Please begin project values from 100
        Project = 100
    };
    
    /// <summary>
    /// Currency that user can collect or buy.
    /// </summary>
    public enum CURRENCY {
         Noads = 0,
         Vip = 1,
         Level = 2,
         // Core reserved values 0-99. Please begin project values from 100
         Project = 100
         };

    /// <summary>
    /// Reason why some events happens. Reason of currency was added or removed.
    /// </summary>
    public enum REASON {
         Iap = 0, 
         Newuser = 1, 
         Ads = 2, 
         Cheat = 3, 
         ProgressRewards = 4, 
         HourlyBonus = 5,
         Buy = 6,
         Vip = 7,
         Refill = 8,
         PuzzleCollection = 9,
         DailyBonus = 10,
         // Core reserved values 0-99. Please begin project values 100
         Project = 100
         };

    /// <summary>
    /// Popup - in game ui dialogs, managed by IPopupManager.
    /// </summary>
    public enum POPUP {
        PurchaseSuccess = 0, 
        PurchaseFailed = 1, 
        ConnectionProblem = 2, 
        Update = 3,
        GDPR = 4,
        Vip = 5,
        NotEnoughCoins = 6,
        Message = 7,
        GDPRForget = 8,
        PuzzlesCollection = 9,
        PuzzleEpisode = 10,
        RateUs = 11,
        DailyBonus = 12,
        OpenReward = 13,
        // Core reserved values 0-99. Please begin project values from from 100
        Project = 100
        };


    /// <summary>
    /// Screen - in game ui screen containers, managed by IScreenService.
    /// </summary>
    public enum SCREEN {
        // Core reserved values 0-99. Please begin project values from from 100
        Project = 100
        };

    //
}