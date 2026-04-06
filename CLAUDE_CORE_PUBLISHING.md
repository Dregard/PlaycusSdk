# Core Publishing Submodule - Technical Documentation for Claude

## Overview

The `submodule-core-publishing` is a modular SDK framework for Unity mobile games that provides essential services for publishing: ads, analytics, in-app purchases, user management, GDPR compliance, and more.

## Architecture

### Core Concepts

1. **Service Pattern**: All services extend `Service` base class and use `ServiceLocator` for dependency injection
2. **ServiceLocator**: Static class that binds/resolves services using `[ServiceBind]` and `[InjectService]` attributes
3. **LoaderAsync**: Orchestrates service initialization in sequence with support for barrier-based grouping
4. **ServiceWithConfig**: Services that load configuration from Firebase Remote Config

### Service States
```csharp
public enum ServiceState
{
    NotStarted = 0,
    Initializing = 1,
    Ready = 2,
    Failed = 3
}
```

### Loading Flow
1. `Loader` binds all child services via `ServiceLocator.BindServicesFromObject()`
2. Resolves dependencies via `ServiceLocator.ResolveServicesInObject()`
3. Activates all GameObjects
4. Calls `LoadAsync()` on each service
5. Services grouped by `----WaitBarrier----` load in parallel within groups, sequentially between groups

---

## Services Reference

### 1. UsercentricsConsentService
**File**: `GDPR/Components/UsercentricsConsentService.cs`
**Binds to**: `UsercentricsConsentService`
**Config**: `UsercentricsConsentServiceConfig`

**Purpose**: Manages GDPR consent and iOS ATT (App Tracking Transparency)

**Key Functionality**:
- Initializes Usercentrics SDK on real devices
- Shows consent dialog (`ShowFirstLayer()`)
- Passes consent status to individual SDKs (Firebase, AppsFlyer, GameAnalytics, Applovin, Meta)
- Handles iOS ATT permission request
- `ForgetMe()` - wipes all user data and quits app

**Template IDs for consent routing**:
- Firebase: `42vRvlulK96R-F`
- Applovin: `fHczTMzX8`
- AppsFlyer: `Gx9iMF__f`
- GameAnalytics: `bQTbuxnTb`
- Amazon APS: `IUyljv4X5`
- Meta Audience Network: `ax0Nljnj2szF_r`

**Events**:
- `GDPRAcceptedEvent` - fired when GDPR accepted
- `ConsendAcceptedEvent` - fired when usercentrics consent processed
- `UserForgetedEvent` - fired when user data deleted

---

### 2. SaveService
**File**: `Saves/SaveService.cs`
**Binds to**: `ISaveService`
**Config**: `SaveServiceConfig`

**Purpose**: Universal save/load system with multiple storage backends

**Key Functionality**:
- Supports multiple storage backends (configured via `SaveServiceConfig.CurrentStore.StoragesPrefabs`)
- Automatically selects storage with highest progress using `CompareProgressKey`
- JSON serialization using `JsonUtility`

**Public API**:
```csharp
// Register object for saving and immediately load its data
await saveService.RegisterAndLoadAsync("uniqueKey", saveVO);

// Trigger save (with 0.5s delay by default)
saveService.Save(forceSaveNow: false);

// Delete all save data
await saveService.DeleteSaveData();
```

**Storage sync flow**:
1. Each storage calls `Synchronize()`
2. When all storages synchronized, selects "current" storage based on progress key
3. Loads data from selected storage

---

### 3. UserInfoServiceLocal
**File**: `User/UserInfoServiceLocal.cs`
**Binds to**: `IUserInfoService`

**Purpose**: Tracks user session information

**Properties**:
- `IsFirstLaunch` - true if SessionCount == 0
- `InstallDate` - first launch date
- `PreviousLaunchDate` - previous session date
- `SessionCount` - total sessions
- `PlayingDaysCount` - unique days played
- `UserID` - persistent UUID (generated on first access)
- `IsNewPlayer` - true if UserID was just generated
- `PlayerName` - editable player name
- `DeepLink` - incoming deep link

**Dependencies**: `ISaveService`

---

### 4. IAPManagerOffline (IapManagerOffline)
**File**: `IAP/IapManagerOffline.cs`
**Binds to**: `IIapManager`
**Config**: `IapManagerOfflineConfig`

**Purpose**: Manages in-app purchases via Unity IAP

**Key Functionality**:
- Requires `UnityServicesInitializer` to be loaded first
- Configures products from `Config.productConfigs`
- Handles consumable, non-consumable, and subscription products
- Tracks subscription trials and renewals
- Validates receipts for subscriptions

**Public API**:
```csharp
void BuyProduct(string productId, string placement);
string GetProductPrice(string productId);
bool IsProductPurchased(string productId);
bool IsProductWasPurchased(string productId);  // anytime purchased
bool IsSubscribed();  // any active subscription
void RestorePurchases();  // iOS only
ProductConfig GetProductConfig(string productId);
```

**Events**:
- `Initialized`
- `PurchaseStarted`
- `PurchaseSuccess(ProductConfig)`
- `PurchaseFailed`

**Dependencies**: `ISaveService`, `IAnalyticsManager`, `INetworkManager`, `UnityServicesInitializer`

---

### 5. AnalyticsService
**File**: `Analytics/AnalyticsService.cs`
**Binds to**: `IAnalyticsManager`
**Config**: `AnalyticsServiceConfig`

**Purpose**: Routes analytics events to multiple platforms

**Key Functionality**:
- Instantiates platform-specific analytics prefabs from config
- Queues events if service not ready, sends in order when ready
- Filters certain events (purchases, ad revenue) for tester users
- Respects GDPR consent state

**Public API**:
```csharp
void CustomEvent(string eventKey, int amount = 0,
    Dictionary<string, object> parameters = null,
    Dictionary<string, object> additionalParameters = null);

void SetGeneralParameterToAllEvents(string parameterKey, object parameterValue);

bool IsTesterUser();
```

**Platform implementations** (in `AnalyticsServicePlatforms/`):
- `FirebaseAnalyticServicePlatform`
- `GameAnalyticsAnalyticServicePlatform`
- `AppsFlyerAnalyticServicePlatform`
- `ByteBrewAnalyticServicePlatform`
- `PlaycusMetricsAnalyticServicePlatform`

---

### 6. AdsService
**File**: `Ads/AdsService.cs`
**Binds to**: `IAdsManager`
**Config**: `AdsServiceConfig`

**Purpose**: Full-featured ads management with placement system, timers, and analytics

**Key Features**:
- Place-based ad configuration (interstitial, rewarded, banner)
- Per-placement timers and click counters
- Session and first-session specific limits
- Integration with IAP for purchase-in-progress blocking
- Auto-mutes audio during ads

**Ad Types**:
- **Rewarded**: `ShowRewarded(PLACE, customReward, onSuccess, onFailure)`
- **Interstitial**: `ShowInterstitial(PLACE, onShowed, onClosed)`
- **Banner**: `ShowBanner(PLACE)`, `HideBanners()`
- **AppOpen**: Internal, shown after certain conditions

**Placement Configuration** (in AdsServiceConfig):
- `_adsPlacesRewarded[]` - rewarded placements with interval limits
- `_adsPlacesInterstitial[]` - interstitial with click counters, intervals
- `_adsPlacesBanner[]` - banner placements with type and position

**Events**:
- `RewardReady`, `RewardStarted(PLACE)`, `RewardCompleted(PLACE, string)`, `RewardErrorShowed(PLACE)`, `RewardCanceled(PLACE)`
- `InterstitialReady`, `InterstitialShowed`, `InterstitialClosed`
- `AppOpenReady`, `AppOpenShowed`, `AppOpenClosed`
- `BannerShowed`, `RectChanged`

**Dependencies**: `IAnalyticsManager`, `IUserInfoService`, `IIapManager`

---

### 7. AdsApi
**File**: `Ads/AdsApi.cs`
**Binds to**: `IAdsApi`
**Config**: `AdsApiConfig`

**Purpose**: Simplified ads API without placement system (uses string placements)

**Key Differences from AdsService**:
- Uses `string` for placement names instead of `PLACE` enum
- No timer/counter system
- Simpler API surface for projects not needing complex placement logic

**Public API**:
```csharp
bool IsInterstitialReady();
void ShowInterstitial(string placement, onShowed, onClosed);

bool IsRewardedReady();
void ShowRewarded(string placement, onSuccess, onFailure);

bool IsBannerReady();
void ShowBanner(string placement, BANNER_TYPE, BANNER_POS);
void HideBanners();

void DisableAd();
```

---

### 8. NetworkManager
**File**: `Network/NetworkManager.cs`
**Binds to**: `INetworkManager`

**Purpose**: Network connectivity monitoring

**Key Functionality**:
- Optional blocking of loading without internet
- `NoNetworkOnCheck` UnityEvent for UI reactions
- Tracks reconnection for analytics

**Public API**:
```csharp
bool CheckNetworkConnection(bool noNetworkActions = true);
```

---

### 9. UnityServicesInitializer
**File**: `UnityServices/UnityServicesInitializer.cs`
**Binds to**: `UnityServicesInitializer`

**Purpose**: Initializes Unity Services (required for IAP)

**Functionality**:
- Calls `UnityServices.InitializeAsync()` with "production" environment
- Must be loaded BEFORE IAPManager

---

### 10. FirebaseService
**Files**: Referenced via prefab, uses Firebase SDK

**Purpose**: Firebase initialization (Analytics, Crashlytics, Remote Config)

---

## Configuration System

### ServiceWithConfig
Services with configs extend `ServiceWithConfig` which:
1. Loads ScriptableObject from `Resources/Playcus/ServiceConfigs/{ServiceName}.asset`
2. Optionally overrides with JSON from Firebase Remote Config using `RemoteVarID`
3. Platform-specific configs via `RemoteVarIDByPlatform`

### Editor Settings
`Playcus/Settings` menu opens `PlaycusSettingsEditorWindow`
- Discovers all `SettingsEntry` subclasses via reflection
- Each entry draws its own GUI

---

## Key Interfaces

### IAdsManager
Full ads management with PLACE enum placements, timers, and counters.

### IAdsApi
Simplified ads API with string placements.

### IAnalyticsManager
```csharp
void CustomEvent(string eventKey, int amount, params);
void SetGeneralParameterToAllEvents(string key, object value);
```

### ISaveService
```csharp
UniTask RegisterAndLoadAsync(string key, object vo);
void Save(bool force);
UniTask DeleteSaveData();
```

### IUserInfoService
User session tracking (sessions, days, install date, user ID).

### IIapManager
In-app purchases (buy, restore, subscription status).

### INetworkManager
```csharp
bool CheckNetworkConnection(bool noNetworkActions);
```

---

## Preprocessor Defines

Key defines used throughout:
- `PL_SDK_FIREBASE_ON` - Firebase enabled
- `PL_SDK_GA_ON` - GameAnalytics enabled
- `PL_SDK_APPSFLYER_ON` - AppsFlyer enabled
- `PL_SDK_FACEBOOK_ON` - Facebook SDK enabled
- `PL_SDK_ADJUST_ON` - Adjust enabled
- `PL_IAP_ON` - Unity IAP enabled
- `PL_USERCENTRICS_CONSENT_ON` - Usercentrics enabled
- `GDPR` - GDPR handling enabled
- `PL_CRASHLYTICS_OFF` - Disable crashlytics logging

---

## LoaderAsync Service Order (from prefab)

1. Usercentrics (GDPR consent)
2. SaveService
3. UserInfoService
4. ----WaitBarrier----
5. IAPManager
6. AnalyticsService
7. NetworkManager (inactive by default)
8. AdsApi (inactive by default)
9. UnityServicesInitializer
10. FirebaseService
11. ----WaitBarrier----

Services before first barrier load in parallel, then wait. Services between barriers load in parallel, etc.

---

## Adding New Services

1. Create class extending `Service` or `ServiceWithConfig`
2. Add `[ServiceBind(typeof(IYourInterface))]` attribute
3. Override `LoadAsyncInternal(CancellationToken)` for async init
4. Call `ServiceLoadingComplete()` when done
5. Use `[InjectService]` for dependencies
6. Add prefab to LoaderAsync hierarchy at appropriate position
