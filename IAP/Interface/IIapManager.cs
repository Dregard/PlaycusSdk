#if PL_IAP_ON
using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Playcus.Iap
{
    public interface IIapManager
    {
        bool IsInitialised { get; }
        (string, string) LastProduct { get; }
        ICollection<Product> Products { get; }

        event Action Initialized;
        event Action PurchaseStarted;
        event Action<Product> PurchaseSuccess;
        event Action<string, PurchaseFailureReason> PurchaseFailed;

        void BuyProduct(string productId, string placement);
        ProductConfig GetProductConfig(string productId);
        ProductConfig FindProduct(string productIDEndMask);
        Product GetProductInformation(string productId);
        string GetProductPrice(string productId);
        bool IsProductPurchased(string productId);
        bool IsProductWasPurchased(string productId);
        bool IsSubscribed();
        void RestorePurchases();
        string GetSubscriptionPriceOff(string productConfigProductId);
    }
}
#endif
