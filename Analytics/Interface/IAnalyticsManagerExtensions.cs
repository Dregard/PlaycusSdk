using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Playcus.Analytics
{
    public static class IAnalyticsManagerExtensions
    {
        private static DateTime _lastTutorialStepTime;

        #region loading
        
        public static void LoadingStart(this IAnalyticsManager service)
        {
            service.CustomEvent(AnalyticsEvents.pl_loading_start.ToString());
        }

        public static void LoadingStep(this IAnalyticsManager service, int stepNumber, int time)
        {
            service.CustomEvent(AnalyticsEvents.pl_loading_step.ToString(), time);
        }

        public static void LoadingEnd(this IAnalyticsManager service, int time)
        {
            service.CustomEvent(AnalyticsEvents.pl_loading_end.ToString(),time);
        }
        
        #endregion

        #region tutorial
        
        public static void TutorialCompleted(this IAnalyticsManager service, int step = 0, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_step.ToString(), step }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_tutorial_completed.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }
        
        public static void TutorialStarted(this IAnalyticsManager service, Dictionary<string, object> customParameters = null)
        {
            service.CustomEvent(
                AnalyticsEvents.pl_tutorial_started.ToString(),
                additionalParameters: customParameters);
        }
        
        public static void TutorialStep(this IAnalyticsManager service, string stepName, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_content_id.ToString(), stepName },
                { AnalyticsProperties.pr_amount.ToString(), (int) (DateTime.Now - _lastTutorialStepTime).TotalSeconds }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_tutorial_step.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);

            _lastTutorialStepTime = DateTime.Now;
        }
        
        #endregion
        
        #region level

        public static void LevelCompleted(this IAnalyticsManager service, int level)
        {
            var eventName = $"pl_level_{level}";
            
            service.CustomEvent(eventName);
        }

        
        // LEGACY
        /* public static void UserLevelAchieved(this IAnalyticsManager service, int level, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_level.ToString(), level },
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_user_level.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }
        
        public static void LevelOpened(this IAnalyticsManager service, int level, string type, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_level.ToString(), level },
                { AnalyticsProperties.pr_level_type.ToString(), type }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_level_opened.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }

        public static void LevelStarted(this IAnalyticsManager service, int level, string type, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_level.ToString(), level },
                { AnalyticsProperties.pr_level_type.ToString(), type }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_level_started.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }

        public static void LevelFailed(this IAnalyticsManager service, int level, string type, int score, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_level.ToString(), level },
                { AnalyticsProperties.pr_level_type.ToString(), type },
                { AnalyticsProperties.pr_level_score.ToString(), score }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_level_failed.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }

        public static void LevelCompleted(this IAnalyticsManager service, int level, string type, int score, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_level.ToString(), level },
                { AnalyticsProperties.pr_level_type.ToString(), type },
                { AnalyticsProperties.pr_level_score.ToString(), score }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_level_completed.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }*/
        
        #endregion

        #region metagame

        public static void NewScore(this IAnalyticsManager service, int level, string type, int score, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_level.ToString(), level },
                { AnalyticsProperties.pr_level_type.ToString(), type },
                { AnalyticsProperties.pr_level_score.ToString(), score }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_new_score.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }

        public static void AchievementUnlocked(this IAnalyticsManager service, string achievementID, Dictionary<string, object> customParameters = null)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_content_id.ToString(), achievementID },
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_achievement.ToString(),
                parameters: eventParameters,
                additionalParameters: customParameters);
        }

        #endregion

        #region remote configs

        public static void LoadingConfigs(this IAnalyticsManager service, string loadingStage, string loadingReason, int iterationCount)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_step.ToString(), loadingStage },
                { AnalyticsProperties.pr_reason.ToString(), loadingReason }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_loading_configs.ToString(),
                amount:iterationCount,
                parameters: eventParameters);
        }

        #endregion

        #region revenue


        public static void AdRevenue(this IAnalyticsManager service, string currency, double revenue, string network,string format, string placement)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_currency.ToString(), currency },
                { AnalyticsProperties.pr_revenue.ToString(), revenue.ToString() },
                { AnalyticsProperties.pr_ad_format.ToString(), format },
                { AnalyticsProperties.pr_ad_network.ToString(), network },
                { AnalyticsProperties.pr_placement.ToString(), placement }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_ads_revenue.ToString(),
                parameters: eventParameters);
        }
        
        public static void PurchaseFailed(this IAnalyticsManager service, Product product, PurchaseFailureReason reason, string placement)
        {
            var currency = product.metadata.isoCurrencyCode;
            var price = product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture);
            var itemID = product.definition.id;
            service.PurchaseFailed(currency, price, itemID, reason, placement);
        }
        
        public static void PurchaseFailed(this IAnalyticsManager service, string currency, string price, string itemID, PurchaseFailureReason reason, string placement)
        {
            if (reason == PurchaseFailureReason.UserCancelled)
            {
                service.PurchaseCanceled(currency, price, itemID, placement);
                return;
            }

            var reasonString = reason.ToString();
            
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_currency.ToString(), currency },
                { AnalyticsProperties.pr_content_id.ToString(), itemID },
                { AnalyticsProperties.pr_placement.ToString(), placement },
                { AnalyticsProperties.pr_reason.ToString(), reasonString },
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_purchase_error.ToString(),
                parameters: eventParameters);

            if (string.IsNullOrEmpty(placement) && service.IsTesterUser())
                Debug.LogError("AnalyticsService: PurchaseFailed placement parameter must be not blank!");
        }

        public static void PurchaseCanceled(this IAnalyticsManager service, Product product, string placement)
        {
            service.PurchaseCanceled(
                product.metadata.isoCurrencyCode,
                product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture),
                product.definition.id,
                placement);
        }
        
        public static void PurchaseCanceled(this IAnalyticsManager service, string currency, string price, string itemID, string placement)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_currency.ToString(), currency },
                { AnalyticsProperties.pr_content_id.ToString(), itemID },
                { AnalyticsProperties.pr_placement.ToString(), placement },
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_purchase_canceled.ToString(),
                parameters: eventParameters
                );

            if (string.IsNullOrEmpty(placement) && service.IsTesterUser())
                Debug.LogError("AnalyticsService: PurchaseCanceled placement parameter must be not blank!");
        }
        
        public static void PurchaseInitiatedCheckout(this IAnalyticsManager service, Product product, string placement)
        {
            service.PurchaseInitiatedCheckout(
                product.metadata.isoCurrencyCode,
                product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture),
                product.definition.id,
                placement.ToString());
        }


        public static void PurchaseInitiatedCheckout(this IAnalyticsManager service, string currency, string price, string itemID, string placement)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_currency.ToString(), currency },
                { AnalyticsProperties.pr_revenue.ToString(), price },
                { AnalyticsProperties.pr_content_id.ToString(), itemID },
                { AnalyticsProperties.pr_placement.ToString(), placement },
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_purchase_checkout.ToString(),
                parameters: eventParameters);
            
            if (string.IsNullOrEmpty(placement) && service.IsTesterUser())
                Debug.LogError("AnalyticsService: PurchaseInitiatedCheckout placement parameter must be not blank!");
        }
        
        public static void PurchaseSuccess(this IAnalyticsManager service, Product product, string placement, string receipt)
        {
            service.PurchaseSuccess(product.metadata.isoCurrencyCode,
                product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture),
                product.definition.id,
                placement,
                receipt
            );
        }

        public static void PurchaseSuccess(this IAnalyticsManager service, string currency, string price, string itemID, string placement, string receipt)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_currency.ToString(), currency },
                { AnalyticsProperties.pr_revenue.ToString(), price },
                { AnalyticsProperties.pr_content_id.ToString(), itemID },
                { AnalyticsProperties.pr_placement.ToString(), placement },
                { AnalyticsProperties.pr_receipt.ToString(), receipt }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_purchase_success.ToString(),
                parameters: eventParameters);
            
            if (string.IsNullOrEmpty(placement) && service.IsTesterUser())
                Debug.LogWarning("AnalyticsService: PurchaseSuccess placement parameter must be not blank!");
        }

        public static void PurchaseSuccess(this IAnalyticsManager service, string currency, string price, string itemID, string itemType, string placement, string receipt)
        {
            var eventParameters = new Dictionary<string, object>()
            {
                { AnalyticsProperties.pr_currency.ToString(), currency },
                { AnalyticsProperties.pr_revenue.ToString(), price },
                { AnalyticsProperties.pr_content_id.ToString(), itemID },
                { AnalyticsProperties.pr_content_type.ToString(), itemType },
                { AnalyticsProperties.pr_placement.ToString(), placement },
                { AnalyticsProperties.pr_receipt.ToString(), receipt }
            };
            
            service.CustomEvent(
                AnalyticsEvents.pl_purchase_success.ToString(), 
                parameters: eventParameters);
            
            if (string.IsNullOrEmpty(placement) && service.IsTesterUser())
                Debug.LogError("AnalyticsService: PurchaseSuccess placement parameter must be not blank!");
        }

        #endregion
    }
}