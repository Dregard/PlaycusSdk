using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if PL_IAP_ON
using UnityEngine.Purchasing;
#endif

namespace Playcus.Iap
{
    public interface IIapManager
    {
        bool IsInitialised { get; }
        (string, string) LastProduct { get; }

        event Action Initialized;
        event Action PurchaseStarted;
        event Action<ProductConfig> PurchaseSuccess;
        event Action PurchaseFailed;

        void BuyProduct(string productId, string placement);
        ProductConfig GetProductConfig(string productId);
        ProductConfig FindProduct(string productIDEndMask);
#if PL_IAP_ON
        Product GetProductInformation(string productId);
#endif
        string GetProductPrice(string productId);
        bool IsProductPurchased(string productId);
        bool IsProductWasPurchased(string productId);
        bool IsSubscribed();
        void RestorePurchases();
        string GetSubscriptionPriceOff(string productConfigProductId);
    }
}