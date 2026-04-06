using UnityEngine;
using System.Collections.Generic;
#if PL_IAP_ON
using UnityEngine.Purchasing;
#endif
using System.Globalization;
using System;
using Playcus.Saves;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Playcus.Network;
using Playcus.Analytics;
using Playcus.Services.Unity;
using Unity.Services.Core;
using UnityEngine.Purchasing.Extension;

namespace Playcus.Iap
{
    /// <summary>
    /// Contain products list and provide methods to perform or restore purchases.
    /// IAnalyticsManager support included with predefined events.
    /// </summary>
    [ServiceBind(typeof(IIapManager))]
    public class IapManagerOffline : ServiceWithConfig, 
        IIapManager
#if PL_IAP_ON
        , IDetailedStoreListener 
#endif
    {
#if PL_IAP_ON
        // EVENTS
        public event Action Initialized;
        public event Action PurchaseStarted;
        public event Action<ProductConfig> PurchaseSuccess;
        public event Action PurchaseFailed;

        // DEPENDENCIES
        [InjectService] private ISaveService _saveManager;
        [InjectService] private IAnalyticsManager _analyticsManager;
        // [InjectService] private ILocalisationManager _localisationManager;
        [InjectService] private INetworkManager _networkManager;
        // [InjectService] private ICurrencyManager _currencyManager;
        // [InjectService] private IItemsService _itemsService;
        // [InjectService] private IPopupsManager _popupsManager;


        // CONFIG
        [HelpBox(
            @"SETUP INSTRUCTION 
- IAPs and ids must be the same for all platforms

- Analytics included with predefined events.

- Subscriptions currencies will be added every renewed period! For vip status use VipManager that depend on subscribe status."
            , HelpBoxMessageType.Info)]
        [SerializeField]
        private bool _readme;

        protected override Type ConfigType => typeof(IapManagerOfflineConfig);
        protected IapManagerOfflineConfig Config => (IapManagerOfflineConfig) _serviceConfig;

        // PRIVATE
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
        private IAppleExtensions _appleExtensions;
        private Dictionary<string, string> _introductory_info_dict;
        private string _environment = "production";

        private string _rememberedProductID;
        private string _rememberedPlace;

        private IapManagerSaveVO _saveVO = new IapManagerSaveVO();

        public (string, string) LastProduct { get; private set; }

        // GET PROPERTIES
        public bool IsInitialised
        {
            get { return _storeController != null && _extensionProvider != null; }
        }

        protected override async UniTask LoadAsyncInternal(CancellationToken cancellationToken)
        {
            await base.LoadAsyncInternal(cancellationToken);
           
            await _saveManager.RegisterAndLoadAsync("IAPManager", _saveVO);
            
            InitializeUnityServices();
        }

        async UniTask InitializeUnityServices()
        {
            if (ServiceLocator.Get<UnityServicesInitializer>() == null)
            {
                Debug.LogError($"IapManager: It is required to add a UnityServicesInitializer to the loader so that it is higher in the hierarchy than the IapManagerOffline", gameObject);
            }
            
            // wait & initialize services
            Debug.Log("IAPManager: Waiting For Initializing UnityServices...", gameObject);
          
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UniTask.WaitUntil(() => UnityServices.State == ServicesInitializationState.Initialized);
            }

            Debug.Log("IAPManager: UnityServices initialization complete", gameObject);
           
            // Event listeners
            // PurchaseSuccess += OnProductPurchaseSuccess;
            // PurchaseFailed += OnProductPurchaseFailed;

            // Initialize IAP
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add all the product ids to the builder
            if (Config.productConfigs != null)
            {
                for (int i = 0; i < Config.productConfigs.Count; i++)
                {
                    ProductConfig productConfig = Config.productConfigs[i];
                    builder.AddProduct(productConfig.productId, productConfig.productType);
                }
            }
            
            Debug.Log("IAPManager: Initializing IAP now...", gameObject);
            UnityPurchasing.Initialize(this, builder);
        }


        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("IAPManager: OnInitialized", gameObject);
            _storeController = controller;
            _extensionProvider = extensions;
            _appleExtensions = extensions.GetExtension<IAppleExtensions>();

            // Subscriptions
            _introductory_info_dict = _appleExtensions.GetIntroductoryPriceDictionary();

            foreach (var item in _storeController.products.all)
            {
                if (item.definition.type == ProductType.Subscription)
                {
                    SubscriptionInfo info = GetSubscriptionInformation(item.definition.id);
                    if (info != null && info.isSubscribed() == Result.True && info.isFreeTrial() == Result.False)
                    {
                        TryTrackSubscriptionCharged(info);
                    }
                }
            }

            Debug.Log("IAPManager: Initialization successful!", gameObject);
            Initialized?.Invoke();

            // If user try to buy before shop inited - continue buy process
            if (!String.IsNullOrEmpty(_rememberedProductID))
                BuyProduct(_rememberedProductID, _rememberedPlace);

            ServiceLoadingComplete();
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError(
                $"IAPManager: Purchase failed for product id: {failureDescription.productId}, reason: {failureDescription.reason}, message: {failureDescription.message}",
                gameObject);
            _analyticsManager.PurchaseFailed(product, failureDescription.reason, _rememberedPlace.ToString());
            PurchaseFailed?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason failureReason)
        {
            Debug.LogError($"IAPManager: Initializion failed! Reason: {failureReason}", gameObject);
            // If user try to buy before shop inited - failed buy process
            if (!String.IsNullOrEmpty(_rememberedProductID))
                PurchaseFailed?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error, string? message)
        {
            Debug.LogError($"IAPManager: Initializion failed! message: {message}", gameObject);
        }


        /// <summary>
        /// Starts the buying process for the given product id
        /// </summary>
        public void BuyProduct(string productId, string placement)
        {
            Debug.Log($"IAPManager: BuyProduct: Purchasing product with id: {productId}", gameObject);

            if (_networkManager != null && !_networkManager.CheckNetworkConnection())
            {
                return;
            }

            PurchaseStarted?.Invoke();
            _rememberedPlace = placement;

            if (IsInitialised)
            {
                Product product = _storeController.products.WithID(productId);
                LastProduct = (productId, placement);

                // If the look up found a product for this device's store and that product is ready to be sold ... 
                if (product == null)
                {
                    Debug.LogError($"IAPManager: BuyProduct: product with id {productId} does not exist.", gameObject);
                    PurchaseFailed?.Invoke();
                } 
                else if (!product.availableToPurchase)
                {
                    Debug.LogError($"IAPManager: BuyProduct: product with id {productId} is not available to purchase.",
                        gameObject);
                    _analyticsManager.PurchaseFailed(product, PurchaseFailureReason.ProductUnavailable, placement.ToString());
                    PurchaseFailed?.Invoke();
                }
                else
                {
                    _analyticsManager.PurchaseInitiatedCheckout(product, placement.ToString());
                    
                    StartCoroutine(InitiatePurchase(product));
                    
#if PL_APPSFLYER_PURCHASE_CONNECTOR_ON && (UNITY_IOS || UNITY_ANDROID)
                    AppsFlyerSDK.AppsFlyer.sendEvent("pl_purchase_checkout",new Dictionary<string, string>()
                    {
                        {AFInAppEvents.CONTENT_ID, product.definition.id}
                    });
#endif
                }
            }
            else
            {
                // Remember product id for continue purchase process after inited
                _rememberedProductID = productId;
                Debug.LogWarning($"IAPManager: BuyProduct: IAPManager not initialized.", gameObject);
            }
        }


        private IEnumerator InitiatePurchase(Product product)
        {
            // Simulate latency in editor
            if (Application.isEditor)
                yield return new WaitForSecondsRealtime(2f);

            // Continue purchasing
            _storeController.InitiatePurchase(product);
        }


        /// <summary>
        /// Buying process was completed 
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // event (args / args.purchasedProduct.transactionID)
#if PL_APPSFLYER_PURCHASE_CONNECTOR_ON
            if (_analyticsManager != null)
            {
                var parameters = new Dictionary<string, object>();
                if (args != null && args.purchasedProduct != null && args.purchasedProduct.transactionID != null)
                {
                    parameters.Add(AnalyticsProperties.pr_content.ToString(), args.purchasedProduct.transactionID);
                }
                else
                {
                    Debug.LogError($"IapManagerOffline: args.purchasedProduct.transactionID is empty", gameObject);
                }
            
                _analyticsManager.CustomEvent("pl_subscription_successful",0,parameters);
            }
            else
            {
                Debug.LogError($"IapManagerOffline: _analyticsManager is empty, ", gameObject);
            }  
#endif
            
            Product product = args.purchasedProduct;
            Debug.Log($"IAPManager: Purchase successful for product id: {product.definition.id}", gameObject);

            ProductConfig productConfig = GetProductConfig(product.definition.id);
            if (productConfig == null)
            {
                Debug.LogError(
                    $"IAPManager: config on prefab not contained productConfig with id: {productConfig.productId}");
                return PurchaseProcessingResult.Pending;
            }

            // Add currencies to user
            // if (productConfig.currencies.Length > 0 || (productConfig.items != null && productConfig.items.Length > 0)
            //     // For subscriptions only first buy give currencies. Constant currencies like statuses can be managed by VipManager
            //     && (product.definition.type != ProductType.Subscription ||
            //         !IsProductWasPurchased(product.definition.id)))
            // {
            //     foreach (CurrencyReward currency in productConfig.currencies)
            //     {
            //         if (_currencyManager != null && currency.count > 0)
            //         {
            //             _currencyManager.AddCurrency(
            //                 ConstantsConvert.StringToCurrency(currency.currencyId),
            //                 currency.count,
            //                 REASON.Iap,
            //                 product.definition.id,
            //                 _rememberedPlace);
            //         }
            //     }
            //
            //     if (_itemsService != null && productConfig.items != null)
            //     {
            //         foreach (ItemReward item in productConfig.items)
            //         {
            //             for (int i = 1; i <= item.Count; i++)
            //             {
            //                 var itemModel = _itemsService.GetItem(item.ItemID, item.ItemGroup);
            //                 itemModel.Count++;
            //             }
            //         }
            //     }
            //
            //     _saveManager.Save();
            // }

            // Remember purchased iaps
            if (!_saveVO.AnytimePurchased.Contains(productConfig.productId))
            {
                _saveVO.AnytimePurchased.Add(productConfig.productId);
                _saveManager.Save();
            }

            // Analytics from client. Only for not subscriptions.
            if (_analyticsManager != null)
            {
                if (product.definition.type != ProductType.Subscription)
                {
                    // Analytics for not subscriptions
                    _analyticsManager.PurchaseSuccess(
                        product.metadata.isoCurrencyCode,
                        product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture),
                        product.definition.id,
                        "", //TODO
                        product.receipt
                    );
                }
                else
                {
                    Debug.Log($"DEBUG SUB purchased");
                    // Subscription status
                    SubscriptionInfo subscriptionInfo = GetSubscriptionInformation(product.definition.id);
                    if (subscriptionInfo != null)
                    {
                        Debug.Log($"DEBUG SUB info get {subscriptionInfo.getProductId()}");
                        
                        if (subscriptionInfo.isFreeTrial() == Result.True)
                        {
                            TrackSubscriptionTrial(subscriptionInfo);
                        }
                        else
                        {
                            TryTrackSubscriptionCharged(subscriptionInfo);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"IAPManager: subscription info is null - can't track analytics");
                    }
                    
                }
            }

            PurchaseSuccess?.Invoke(GetProductConfig(product.definition.id));
            return PurchaseProcessingResult.Complete;
        }


        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError(
                $"IAPManager: Purchase failed for product id: {product.definition.id}, reason: {failureReason}",
                gameObject);
            _analyticsManager.PurchaseFailed(product, failureReason, _rememberedPlace.ToString());
            PurchaseFailed?.Invoke();
        }

        // private void OnProductPurchaseFailed()
        // {
        //     // generic MessagePopup
        //     if (_popupsManager != null)
        //     {
        //         _popupsManager.ShowPopupAsync(POPUP.PurchaseFailed, PopupShow.STACK, "purchaseFailedTitle",
        //             "purchaseFailedDesc");
        //     }
        // }
        //
        //
        // private void OnProductPurchaseSuccess(ProductConfig config)
        // {
        //     // generic MessagePopup
        //     if (_popupsManager != null)
        //     {
        //         _popupsManager.ShowPopupAsync(POPUP.PurchaseSuccess, PopupShow.STACK, "purchasedTitle",
        //             "purchasedDesc", config);
        //     }
        // }

        /// <summary>
        /// Is product in purchased status? Subscription will calculate expaired period.
        /// </summary>
        public bool IsProductPurchased(string productId)
        {
            Product product = GetProductInformation(productId);
            if (product != null)
            {
                if (product.definition.type == ProductType.NonConsumable && product.hasReceipt)
                {
                    // Owned Non Consumables  should always have receipts.
                    // So here the Non Consumable product has already been bought.
                    return true;
                }

                if (product.definition.type == ProductType.Subscription && product.hasReceipt)
                {
                    // Subscription receipt need be checked for expired period
                    SubscriptionInfo subscription = GetSubscriptionInformation(productId);
                    if (subscription != null && subscription.isSubscribed() == Result.True &&
                        subscription.isExpired() != Result.True)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Is product was purchased anytime?
        /// </summary>
        public bool IsProductWasPurchased(string productId)
        {
            return _saveVO.AnytimePurchased.Contains(productId);
        }

        /// <summary>
        /// Is user has any active subscription?
        /// </summary>
        public bool IsSubscribed()
        {
            foreach (var item in _storeController.products.all)
            {
                if (item.definition.type == ProductType.Subscription)
                {
                    if (IsProductPurchased(item.definition.id))
                        return true;
                }
            }

            return false;
        }

       

        /// <summary>
        /// Gets the products store information
        /// </summary>
        public Product GetProductInformation(string productId)
        {
            if (IsInitialised)
            {
                return _storeController.products.WithID(productId);
            }

            return null;
        }

        /// <summary>
        /// Gets the products store information
        /// </summary>
        public ProductConfig GetProductConfig(string productId)
        {
            foreach (ProductConfig item in Config.productConfigs)
            {
                if (item.productId == productId)
                    return item;
            }
            Debug.LogWarning($"IapManagerOffline : GetProductConfig : Can't find product with name = '{productId}'");
            return null;
        }
        
        public ProductConfig FindProduct(string productIDEndMask)
        {
            foreach (ProductConfig item in Config.productConfigs)
            {
                if (item.productId.EndsWith(productIDEndMask))
                    return item;
            }
            Debug.LogWarning($"IapManagerOffline : GetProductConfig : Can't find product by name mask = '{productIDEndMask}'");
            return null;
        }

        /// <summary>
        /// Return product price in local currency or manual setuped (if offline)
        /// </summary>
        public string GetProductPrice(string productId)
        {
            Product product = GetProductInformation(productId);
            ProductConfig productConfig = GetProductConfig(productId);
            string productPrice = "";
            if (Application.isEditor || product == null ||
                string.IsNullOrEmpty(product.metadata.localizedPriceString) ||
                product.metadata.localizedPriceString == "0")
            {
                if (productConfig != null)
                    productPrice = "$" + productConfig.priceManualUSD;
                else
                    productPrice = "...";
            }
            else
            {
                if (product.metadata.isoCurrencyCode == "USD")
                {
                    // Special format for USD
                    productPrice = "$" + product.metadata.localizedPrice;
                }
                else
                {
                    productPrice = product.metadata.localizedPriceString;
                }
            }

            return productPrice;
        }


        /// <summary>
        /// Return subscription offer price in period
        /// </summary>
        public string GetSubscriptionPriceOff(string productId)
        {
            Product product = GetProductInformation(productId);
            ProductConfig productConfig = GetProductConfig(productId);
            string productOfferPrice = "";

            if (productConfig != null && productConfig.productType == ProductType.Subscription)
            {
                float productFullPrice = 0.0f;
                string productCode = "";
                ProductConfig.SubscriptionPeriod offPeriod = ProductConfig.SubscriptionPeriod.Week;
                float offParts = 1f;

                if (Application.isEditor || product == null ||
                    string.IsNullOrEmpty(product.metadata.localizedPriceString) ||
                    product.metadata.localizedPriceString == "0")
                {
                    productFullPrice = productConfig.priceManualUSD;
                    productCode = "$";
                }
                else
                {
                    if (product.metadata.isoCurrencyCode == "USD")
                    {
                        productCode = "$";
                    }
                    else
                    {
                        productCode = product.metadata.isoCurrencyCode;
                    }

                    productFullPrice = Convert.ToSingle(product.metadata.localizedPrice);
                }

                // Calculate off price by previous periods
                if (productConfig.subscriptionPeriod == ProductConfig.SubscriptionPeriod.Month)
                {
                    offPeriod = ProductConfig.SubscriptionPeriod.Week;
                    offParts = 4f;
                }

                if (productConfig.subscriptionPeriod == ProductConfig.SubscriptionPeriod.Month2)
                {
                    offPeriod = ProductConfig.SubscriptionPeriod.Week;
                    offParts = 8f;
                }

                if (productConfig.subscriptionPeriod == ProductConfig.SubscriptionPeriod.Month3)
                {
                    offPeriod = ProductConfig.SubscriptionPeriod.Week;
                    offParts = 12f;
                }

                if (productConfig.subscriptionPeriod == ProductConfig.SubscriptionPeriod.Month6)
                {
                    offPeriod = ProductConfig.SubscriptionPeriod.Week;
                    offParts = 24f;
                }

                if (productConfig.subscriptionPeriod == ProductConfig.SubscriptionPeriod.Year)
                {
                    offPeriod = ProductConfig.SubscriptionPeriod.Month;
                    offParts = 12f;
                }

                // productOfferPrice =
                //     $"{productCode} {(productFullPrice / offParts).ToString("0.00")} / {_localisationManager.GetTranslation(offPeriod.ToString())}";
                productOfferPrice =
                    $"{productCode} {(productFullPrice / offParts).ToString("0.00")} / {offPeriod}";
            }

            return productOfferPrice;
        }


        /// <summary>
        /// Restores the purchases if platform is iOS or OSX
        /// </summary>
        public void RestorePurchases()
        {
            Debug.Log("IAPManager: RestorePurchases: Restoring purchases", gameObject);
            if (IsInitialised)
            {
                if ((Application.platform == RuntimePlatform.IPhonePlayer ||
                     Application.platform == RuntimePlatform.OSXPlayer)
                )
                {
                    // PurchaseStarted?.Invoke();
                    _extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((result) =>
                    {
                        if (result == false)
                        {
                            PurchaseFailed?.Invoke();
                        }
                    });
                }
                else
                {
                    Debug.LogWarning("IAPManager: RestorePurchases: Device is not iOS, no need to call this method.",
                        gameObject);
                }
            }
            else
            {
                Debug.LogWarning("IAPManager: RestorePurchases: IAPManager not initialized.", gameObject);
            }
        }


        /// <summary>
        /// Gets the subscription information
        /// </summary>
        private SubscriptionInfo GetSubscriptionInformation(string productId)
        {
            if (IsInitialised)
            {
                Product item = GetProductInformation(productId);
                if (item != null && item.availableToPurchase)
                {
                    // this is the usage of SubscriptionManager class
                    if (item.receipt != null)
                    {
                        if (item.definition.type == ProductType.Subscription)
                        {
                            if (checkIfProductIsAvailableForSubscriptionManager(item.receipt))
                            {
                                string intro_json =
                                    (_introductory_info_dict == null ||
                                     !_introductory_info_dict.ContainsKey(item.definition.storeSpecificId))
                                        ? null
                                        : _introductory_info_dict[item.definition.storeSpecificId];

                                SubscriptionManager p = new SubscriptionManager(item, intro_json);
                                SubscriptionInfo info = p.getSubscriptionInfo();
                                if (Debug.isDebugBuild)
                                {
                                    Debug.Log("IAPManager: subscription: product id is: " + info.getProductId());
                                    Debug.Log("IAPManager: subscription: purchase date is: " + info.getPurchaseDate());
                                    Debug.Log("IAPManager: subscription: subscription next billing date is: " +
                                              info.getExpireDate());
                                    Debug.Log("IAPManager: subscription: is subscribed? " +
                                              info.isSubscribed().ToString());
                                    Debug.Log("IAPManager: subscription: is expired? " + info.isExpired().ToString());
                                    Debug.Log("IAPManager: subscription: is cancelled? " + info.isCancelled());
                                    Debug.Log("IAPManager: subscription: product is in free trial peroid? " +
                                              info.isFreeTrial());
                                    Debug.Log("IAPManager: subscription: product is auto renewing? " +
                                              info.isAutoRenewing());
                                    Debug.Log(
                                        "IAPManager: subscription: subscription remaining valid time until next billing date is: " +
                                        info.getRemainingTime());
                                    Debug.Log(
                                        "IAPManager: subscription: is this product in introductory price period? " +
                                        info.isIntroductoryPricePeriod());
                                    Debug.Log(
                                        "IAPManager: subscription: the product introductory localized price is: " +
                                        info.getIntroductoryPrice());
                                    Debug.Log("IAPManager: subscription: the product introductory price period is: " +
                                              info.getIntroductoryPricePeriod());
                                    Debug.Log(
                                        "IAPManager: subscription: the number of product introductory price period cycles is: " +
                                        info.getIntroductoryPricePeriodCycles());
                                }

                                return info;
                            }
                            else
                            {
                                Debug.LogWarning(
                                    "This product is not available for SubscriptionManager class, only products that are purchase by 1.19+ SDK can use this class.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("the product is not a subscription product");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("the product should have a valid receipt");
                    }
                }
            }

            return null;
        }

        private bool checkIfProductIsAvailableForSubscriptionManager(string receipt)
        {
            Debug.Log($"checkIfProductIsAvailableForSubscriptionManager receipt ended!");

            var receipt_wrapper = (Dictionary<string, object>) MiniJson.JsonDecode(receipt);
            if (!receipt_wrapper.ContainsKey("Store") || !receipt_wrapper.ContainsKey("Payload"))
            {
                Debug.Log("The product receipt does not contain enough information");
                return false;
            }

            var store = (string) receipt_wrapper["Store"];
            var payload = (string) receipt_wrapper["Payload"];

            if (payload != null)
            {
                switch (store)
                {
                    case GooglePlay.Name:
                    {
                        var payload_wrapper = (Dictionary<string, object>) MiniJson.JsonDecode(payload);

                        #region INNER FUNCTIONS

                        bool IsReceiptGoodForSubOld()
                        {
                            string firstLayerTag = "json";
                            string secondLayerTag = "developerPayload";

                            if (!payload_wrapper.ContainsKey(firstLayerTag))
                            {
                                Debug.Log(
                                    $"The product receipt does not contain enough information, the firstLayerTag {firstLayerTag} field is missing");
                                return false;
                            }


                            var firstLayerWrapper =
                                (Dictionary<string, object>) MiniJson.JsonDecode(
                                    (string) payload_wrapper[firstLayerTag]);


                            if (firstLayerWrapper == null || !firstLayerWrapper.ContainsKey(secondLayerTag))
                            {
                                Debug.Log(
                                    $"The product receipt does not contain enough information, the secondLayerTag {secondLayerTag} field is missing");
                                return false;
                            }


                            var secondLayerJSON = (string) firstLayerWrapper[secondLayerTag];
                            var secondLayerWrapper = (Dictionary<string, object>) MiniJson.JsonDecode(secondLayerJSON);

                            if (secondLayerWrapper != null
                                || !secondLayerWrapper.ContainsKey("is_free_trial")
                                || !secondLayerWrapper.ContainsKey("has_introductory_price_trial"))
                            {
                                Debug.Log(
                                    "The product receipt does not contain enough information, the product is not purchased using version between 1.19 and 3.0.2");
                                return false;
                            }

                            return true;
                        }

                        bool IsReceiptGoodForSubNew()
                        {
                            string firstLayerTag = "skuDetails";

                            if (!payload_wrapper.ContainsKey(firstLayerTag))
                            {
                                Debug.Log(
                                    $"The product receipt does not contain enough information, the firstLayerTag {firstLayerTag} field is missing");
                                return false;
                            }

                            string payload_wrapper_data = "";
                            try
                            {
                                 payload_wrapper_data = (string)payload_wrapper[firstLayerTag];
                            }
                            catch (System.InvalidCastException)
                            {
                                try
                                {
                                    List<object> list_data = (List<object>)payload_wrapper[firstLayerTag];
                                    payload_wrapper_data = (string) list_data[0];
                                }
                                catch (System.InvalidCastException)
                                {
                                    UnityEngine.Debug.LogError(
                                        $"checkIfProductIsAvailableForSubscriptionManager skuDetails Invalid Receipt Cast");
                                    return false;
                                }
                               
                            }

                            if (string.IsNullOrEmpty(payload_wrapper_data))
                            {
                                UnityEngine.Debug.LogError(
                                    $"checkIfProductIsAvailableForSubscriptionManager skuDetails Data string empty Invalid Receipt Cast");
                                return false;
                            }
                            
                            var firstLayerWrapper =
                                (Dictionary<string, object>) MiniJson.JsonDecode(payload_wrapper_data);

                            if (firstLayerWrapper == null)
                            {
                                UnityEngine.Debug.Log(
                                    $"checkIfProductIsAvailableForSubscriptionManager firstLayerWrapper == null. return false");
                                return false;
                            }

                            bool isWrapperLegit = firstLayerWrapper.ContainsKey("freeTrialPeriod") ||
                                                  firstLayerWrapper.ContainsKey("subscriptionPeriod");

                            if (isWrapperLegit)
                            {
                                UnityEngine.Debug.Log(
                                    $"checkIfProductIsAvailableForSubscriptionManager isWrapperLegit. Return true");
                                return true;
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning(
                                    $"checkIfProductIsAvailableForSubscriptionManager isWrapperLegit == false. Return false");
                                return false;
                            }
                        }

                        #endregion

                        return (IsReceiptGoodForSubOld() || IsReceiptGoodForSubNew());
                    }
                    case AppleAppStore.Name:
                    case AmazonApps.Name:
                    case MacAppStore.Name:
                    {
                        return true;
                    }
                    default:
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private void TrackSubscriptionTrial(SubscriptionInfo info)
        {
            Debug.Log($"DEBUG SUB TrackSubscriptionTrial");
            // trial started
            _analyticsManager.CustomEvent(AnalyticsEvents.pl_subscription_trial.ToString(), 0,
                new Dictionary<string, object>()
                {
                    {AnalyticsProperties.pr_content_id.ToString(), info.getProductId()}
                }
            );
        }

        private void TryTrackSubscriptionCharged(SubscriptionInfo info)
        {
            Debug.Log($"DEBUG SUB TryTrackSubscriptionCharged");
            string productId = info.getProductId();
            // subscription without trial one time tracked and remember
            if (!_saveVO.AnalyticsSubscriptionTracked.Contains(productId))
            {
                _analyticsManager.CustomEvent(AnalyticsEvents.pl_subscription_charged.ToString(), 0,
                    new Dictionary<string, object>()
                    {
                        {AnalyticsProperties.pr_content_id.ToString(), productId}
                    }
                );
                _saveVO.AnalyticsSubscriptionTracked.Add(productId);
                _saveManager.Save();
            }
        }
#else
        protected override Type ConfigType { get; }
        public bool IsInitialised { get; }
        public (string, string) LastProduct { get; }
        public event Action Initialized;
        public event Action PurchaseStarted;
        public event Action<ProductConfig> PurchaseSuccess;
        public event Action PurchaseFailed;
        public void BuyProduct(string productId, string placement)
        {
        }

        public ProductConfig GetProductConfig(string productId)
        {
            return null;
        }

        public ProductConfig FindProduct(string productIDEndMask)
        {
            return null;
        }

#if PL_IAP_ON
        public Product GetProductInformation(string productId)
        {
        }
#endif

        public string GetProductPrice(string productId)
        {
            return null;
        }

        public bool IsProductPurchased(string productId)
        {
            return false;
        }

        public bool IsProductWasPurchased(string productId)
        {
            return false;
        }

        public bool IsSubscribed()
        {
            return false;
        }

        public void RestorePurchases()
        {
        }

        public string GetSubscriptionPriceOff(string productConfigProductId)
        {
            return null;
        }
#endif
    }
}