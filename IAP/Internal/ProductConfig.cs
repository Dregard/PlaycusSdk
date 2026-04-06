using UnityEngine;
using UnityEngine.Purchasing;

namespace Playcus.Iap
{
    [System.Serializable]
    public class ProductConfig
    {
        [Tooltip("Product id in store. Must be the same for all platforms")]
        public string productId;

        [Tooltip("Consumable - can be purchased many times. Nonconsumable - one time purchase. Subscription - repeated purchase.")]
        public ProductType productType;

        [Tooltip("Price that will be showed without internet")]
        public float priceManualUSD;

        [Tooltip("How long subscription.")]
        public SubscriptionPeriod subscriptionPeriod;

        [Tooltip("(Optional) How long subscription will be free.")]
        public int subscriptionTrialDays;

        [Tooltip("(Optional) What price will be showed as previous price (sale old price)")]
        public string productIdOldPrice;

        [Tooltip("(Optional) What % will be showed as get more currency free (sale more)")]
        public int currenciesMoreBonus;

        [Tooltip("(Optional) For any purpose what you want... come on don't do it!")]
        public string customGoods;

        public enum SubscriptionPeriod { None, Week, Month, Month2, Month3, Month6, Year };
    }
}