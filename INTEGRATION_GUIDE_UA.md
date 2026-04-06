# Core Publishing — Інструкція з інтеграції

---

# Частина 1: Базова інтеграція

## 1. Підключення сабмодуля

```bash
git submodule add <repository-url> Assets/submodule-core-publishing
git submodule update --init --recursive
```

Після клонування репозиторію з існуючим сабмодулем:
```bash
git submodule update --init --recursive
```

> **Важливо:** Вміст сабмодуля потрібно ложити в папку `Assets/submodule-core-publishing/'

## 2. Додавання Loader сцени

1. Знайдіть в сабмодулі сцену `LoaderAsyncScene` і встановіть її як **Build Index 0** у `File > Build Settings`
4. Переконайтесь що ваша ігрова сцена має **Build Index 1** — Loader автоматично завантажить її після ініціалізації сервісів

> **Важливо:** Loader має бути тільки на сцені завантаження. Ніколи не дублюйте сервіси Loader на ігрових сценах.

---

## 3. Налаштування Playcus > Settings

Відкрийте вікно: **Menu → Playcus → Settings**

> **Важливо:** Після кожного увімкнення/вимкнення toggle — дочекайтесь перекомпіляції скриптів (зміна scripting define symbols). Не продовжуйте налаштування поки Unity не закінчить компіляцію.

---

### 3.1 Analytics

Підтримувані платформи аналітики:

| Платформа | Define Symbol | Що потрібно |
|-----------|--------------|-------------|
| **AppsFlyer** | `PL_SDK_APPSFLYER_ON` | Dev Key, iOS App ID, Google Public Key |
| **GameAnalytics** | `PL_SDK_GA_ON` | Налаштовується через окремий SDK GameAnalytics |
| **ByteBrew** | `PL_BYTEBREW_ANALYTICS_ON` | Автоматична ініціалізація, без додаткових ключів |
| **DevToDev** | `PL_SDK_D2D_ON` | iOS App ID, Android App ID |

> **Важливо:** якщо у вас не підключена якась з платформ аналітики, то вмикати її не потрібно

#### AppsFlyer
- **AppsflyerDevKey** — ключ розробника. Отримати: [AppsFlyer Dashboard](https://hq1.appsflyer.com/) → App Settings → Dev Key
- **IosAppNumberID** — числовий ID додатку в App Store (тільки для iOS). Отримати: [App Store Connect](https://appstoreconnect.apple.com/) → General → Apple ID
- **GooglePublicKey** — публічний ключ для валідації рецептів на Android. Отримати: [Google Play Console](https://play.google.com/console/) → Monetize → Monetization setup → Licensing

#### GameAnalytics
- Конфігурується через власне вікно GameAnalytics SDK. Ключі отримати: [GameAnalytics Dashboard](https://gameanalytics.com/) → Settings → Keys

#### ByteBrew
- Без додаткових полів. Ключі налаштовуються в [ByteBrew Dashboard](https://dashboard.bytebrew.io/)

#### DevToDev
- **iOSAppID** / **androidAppID** — ID додатку для кожної платформи. Отримати: [DevToDev Dashboard](https://www.devtodev.com/) → App Settings

---

### 3.2 Ads (Реклама)

| Параметр | Define Symbol | Опис |
|----------|--------------|------|
| **AppLovin** | `PL_SDK_APPLOVIN_ON` | Основна рекламна мережа |
| **Amazon TAM** | `PL_AMAZON_TAM_ON` | Amazon Transparent Ad Marketplace (додатковий медіатор) | //поки завжди лишаємо вимкненим

#### AppLovin — налаштування для кожної платформи (iOS / Android):
- **SDKKey** — ключ AppLovin SDK. Отримати: [AppLovin MAX Dashboard](https://dash.applovin.com/) → Account → Keys → SDK Key
- **RewardedUnitID** — Ad Unit ID для rewarded відео. Створити: MAX Dashboard → Manage → Ad Units → Create Ad Unit (Rewarded)
- **InterstitialUnitID** — Ad Unit ID для interstitial. Створити: MAX Dashboard → Ad Units → Create (Interstitial)
- **BannerUnitID** — Ad Unit ID для банера. Створити: MAX Dashboard → Ad Units → Create (Banner)
- **AppOpenUnitID** — Ad Unit ID для App Open реклами

#### Типи реклами:
- **Rewarded** — відео з нагородою (гравець дивиться → отримує бонус)
- **Interstitial** — повноекранна реклама між сценами/рівнями
- **Banner** — постійний банер (BANNER/MREC), позиція: TOP або BOTTOM
- **App Open** — реклама при поверненні в додаток

#### Додаткові налаштування конфігу:
- `RewardedEnabled` / `InterstitialEnabled` / `BannerEnabled` — увімкнення типів реклами
- `DefaultBannerPosition` — позиція банера за замовчуванням (TOP / BOTTOM)
- `AppOpenEnabledOnStart` — показувати App Open при старті

#### Amazon TAM (якщо увімкнено):
- **appId** — ID додатку в Amazon
- **amazonBannerSlotId**, **amazonInterstitialSlotId**, **amazonRewardedVideoSlotId** — slot ID для кожного типу реклами

---

### 3.3 IAP (In-App Purchases)

| Параметр | Define Symbol | Опис |
|----------|--------------|------|
| **IAP** | `PL_IAP_ON` | Увімкнення Unity Purchasing |
| **AppsFlyer Purchase Connector** | `PL_APPSFLYER_PURCHASE_CONNECTOR_ON` | Трекінг покупок через AppsFlyer |

#### Налаштування продуктів

Кожен продукт (`ProductConfig`) має поля:

| Поле | Опис |
|------|------|
| `productId` | ID продукту — **має збігатись** з ID в Google Play Console / App Store Connect |
| `productType` | `Consumable`, `Nonconsumable`, `Subscription` |
| `priceManualUSD` | Fallback ціна в USD (якщо немає інтернету) |
| `subscriptionPeriod` | Для підписок: `Week`, `Month`, `Month2`, `Month3`, `Month6`, `Year` |
| `subscriptionTrialDays` | Тріальний період в днях |
| `productIdOldPrice` | ID продукту для показу "стара ціна" (знижки) |
| `currenciesMoreBonus` | Відсоток бонусу для UI |
| `customGoods` | Довільні метадані |

**Де створювати продукти:**
- **Android:** [Google Play Console](https://play.google.com/console/) → Monetize → Products → In-app products / Subscriptions
- **iOS:** [App Store Connect](https://appstoreconnect.apple.com/) → In-App Purchases

> ID продукту має бути однаковим для обох платформ (наприклад, `com.company.game.coins_100`).

---

### 3.4 CMP / GDPR (Consent Management)

| Параметр | Define Symbol | Опис |
|----------|--------------|------|
| **Usercentrics** | `PL_USERCENTRICS_CONSENT_ON` | GDPR/CCPA compliance |

#### Конфігурація:
- **Settings ID** — отримати: [Usercentrics Admin Interface](https://admin.usercentrics.eu/) → Implementation → Settings ID
- `versionTos` — версія Terms of Service (0 = вимкнено, ≥1 = показується і перевіряється)
- `eighteenToggleChecked` — чи toggle активований за замовчуванням
- `manualAuditGDPR` — ручний контроль показу GDPR діалогу

---

### 3.5 Firebase

Firebase не має окремого toggle в Settings — він вмикається через scripting define symbols:

| Define Symbol | Опис |
|--------------|------|
| `PL_SDK_FIREBASE_ON` | Увімкнення Firebase SDK (Analytics, Crashlytics, Dependencies) |
| `PL_FIREBASE_REMOTE_CONFIGS_ON` | Увімкнення Firebase Remote Config |
| `PL_CRASHLYTICS_OFF` | Вимкнення Crashlytics (за потреби) |

#### Підключення:
1. Додайте `PL_SDK_FIREBASE_ON` до scripting define symbols: `Edit > Project Settings > Player > Scripting Define Symbols`
2. Для Remote Config додатково: `PL_FIREBASE_REMOTE_CONFIGS_ON`
3. Дочекайтесь перекомпіляції

> Firebase конфігурація (`google-services.json` для Android, `GoogleService-Info.plist` для iOS) має бути в проекті згідно [Firebase документації](https://firebase.google.com/docs/unity/setup).

---

## 4.Далі потрібно переписати всі визови для вищевказаних сервісів з коду гри( Приклади виклику з коду)

### 4.1 Analytics

```csharp
using Playcus.Analytics;

var analytics = ServiceLocator.Get<IAnalyticsManager>();

// Перевірка тестового режиму
if (analytics.IsTesterUser()) { /* тестовий юзер */ }

```
---

### 4.2 Ads (IAdsApi)

```csharp
using Playcus.Ads;

var ads = ServiceLocator.Get<IAdsApi>();

// --- Rewarded ---
if (ads.IsRewardedReady())
{
    ads.ShowRewarded("double_coins",
        onSuccess: () => { /* Нагорода гравцю */ },
        onFailure: () => { /* Скасовано або помилка */ }
    );
}

// --- Interstitial ---
if (ads.IsInterstitialReady())
{
    ads.ShowInterstitial("level_complete",
        onShowed: () => { Time.timeScale = 0; },
        onClosed: () => { Time.timeScale = 1; }
    );
}

// --- Banner ---
if (ads.IsBannerReady())
{
    ads.ShowBanner("main_menu", BANNER_TYPE.BANNER, BANNER_POS.BOTTOM);
}
ads.HideBanners();

// --- Вимкнення реклами (після покупки No Ads) ---
ads.DisableAd();
```

**Події:**
```csharp
ads.RewardReady += () => { /* Rewarded завантажено */ };
ads.RewardCompleted += (placement) => { /* Rewarded завершено */ };
ads.InterstitialClosed += () => { /* Interstitial закрито */ };
ads.BannerShowed += () => { /* Банер показано */ };
```

---

### 4.3 IAP

```csharp
using Playcus.Iap;

var iap = ServiceLocator.Get<IIapManager>();

// Покупка
iap.BuyProduct("com.company.game.coins_100", "shop_main");

// Обробка результатів
iap.PurchaseStarted += () => { ShowLoading(); };
iap.PurchaseSuccess += (ProductConfig product) =>
{
    HideLoading();
    // Видати нагороду на основі product
};
iap.PurchaseFailed += () => { HideLoading(); ShowError(); };

// Ціна з локалізацією
string price = iap.GetProductPrice("com.company.game.coins_100"); // "$0.99" або "29 ₴"

// Конфігурація продукту
ProductConfig config = iap.GetProductConfig("com.company.game.coins_100");

// Підписки
if (iap.IsSubscribed()) { /* Увімкнути premium */ }
if (iap.IsProductPurchased("vip_monthly")) { /* Активна підписка */ }
if (iap.IsProductWasPurchased("starter_pack")) { /* Вже купувалось */ }

// Відновлення (iOS)
iap.RestorePurchases();
```

---



### 4.4 Consent (GDPR)

```csharp
using Playcus.GDPR;

var consent = ServiceLocator.Get<UsercentricsConsentService>();

if (consent.IsGDPRAccepted()) { /* Згода надана */ }

// Події
UsercentricsConsentService.GDPRAcceptedEvent += () => { /* Згода отримана */ };
UsercentricsConsentService.UserForgetedEvent += () => { /* Дані видалені */ };

// Видалення даних користувача (закриває додаток)
await UsercentricsConsentService.ForgetMe();
```

---

## 5. Dependency Injection

Два способи отримати сервіс:

### Спосіб 1: ServiceLocator.Get<T>()
```csharp
var analytics = ServiceLocator.Get<IAnalyticsManager>();
var ads = ServiceLocator.Get<IAdsApi>();
```

### Спосіб 2: [InjectService] атрибут
```csharp
public class MyController : MonoBehaviour
{
    [InjectService] private IAnalyticsManager _analytics;
    [InjectService] private IAdsApi _ads;
    [InjectService] private ISaveService _save;

    void Start()
    {
        // Сервіси вже inject'нуті (якщо об'єкт був resolved через ServiceLocator)
        _analytics.CustomEvent("game_started");
    }
}
```

Для `[InjectService]` сервіси inject'яться автоматично якщо:
- Об'єкт є дочірнім об'єктом Loader (resolve відбувається в Loader.Awake)
- Або ви викликали `ServiceLocator.ResolveServicesInComponent(this)` вручну

---


## Повний список Scripting Define Symbols

| Define | Сервіс | Опис |
|--------|--------|------|
| `PL_SDK_APPLOVIN_ON` | Ads | AppLovin рекламна мережа |
| `PL_AMAZON_TAM_ON` | Ads | Amazon TAM медіатор |
| `PL_SDK_APPSFLYER_ON` | Analytics | AppsFlyer аналітика |
| `PL_SDK_GA_ON` | Analytics | GameAnalytics |
| `PL_BYTEBREW_ANALYTICS_ON` | Analytics | ByteBrew аналітика |
| `PL_SDK_D2D_ON` | Analytics | DevToDev аналітика |
| `PL_SDK_PLAYCUSDATALAKE_ON` | Analytics | Playcus Data Lake |
| `PL_SDK_ADJUST_ON` | Analytics | Adjust аналітика |
| `PL_IAP_ON` | IAP | Unity In-App Purchases |
| `PL_APPSFLYER_PURCHASE_CONNECTOR_ON` | IAP | AppsFlyer Purchase Connector |
| `PL_USERCENTRICS_CONSENT_ON` | GDPR | Usercentrics CMP |
| `PL_SDK_FIREBASE_ON` | Firebase | Firebase SDK |
| `PL_FIREBASE_REMOTE_CONFIGS_ON` | Firebase | Firebase Remote Config |
| `PL_CRASHLYTICS_OFF` | Firebase | Вимкнення Crashlytics |
