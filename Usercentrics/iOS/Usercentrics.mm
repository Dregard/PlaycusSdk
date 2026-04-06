#import <Foundation/Foundation.h>
#import <Usercentrics/Usercentrics.h>
#import <UsercentricsUI/UsercentricsUI-Swift.h>
#import <UIKit/UIKit.h>
#import <UnityFramework/UnityFramework.h>

extern UIViewController *UnityGetGLViewController();
extern char *toChar(NSString *value);

@interface UsercentricsHelper : NSObject
+(void)sendUnityMessageWithObj:(NSString *)obj andMethod:(NSString*)method andMsg:(NSString*)msg;
@end

@implementation UsercentricsHelper
+(void)sendUnityMessageWithObj:(NSString *)obj andMethod:(NSString*)method andMsg:(NSString*)msg {
    UnitySendMessage(toChar(obj), toChar(method), toChar(msg));
}
@end

#pragma mark - C interface
char *toChar(NSString *value) {
    char *string = (char *)[value UTF8String];
    if (string == nil)
        return NULL;
    char* copy = (char*)malloc(strlen(string) + 1);
    strcpy(copy, string);
    return copy;
}

NSString* _Nonnull UcCreateNSString(const char* string) {
    return [NSString stringWithUTF8String:string ?: ""];
}

extern "C" {

    void ucInitCMP(const char* initialArgs) {
        [UsercentricsResourceLoader setBundleInput:[NSBundle bundleForClass:[UnityFramework class]]];
        [[UsercentricsUsercentricsUnityCompanion companion] doInitAppContext:nil rawUnityUserOptions:UcCreateNSString(initialArgs)];
    }

    void ucShowFirstLayer(const char* bannerSettingsJson) {
        UsercentricsUnityBanner *banner = [[UsercentricsUnityBanner alloc] init];
        [banner showFirstLayerWithHostView:UnityGetGLViewController() bannerSettings:UcCreateNSString(bannerSettingsJson)];
    }

    void ucShowSecondLayer(const char* bannerSettingsJson) {
        UsercentricsUnityBanner *banner = [[UsercentricsUnityBanner alloc] init];
        [banner showSecondLayerWithHostView:UnityGetGLViewController() bannerSettings:UcCreateNSString(bannerSettingsJson)];
    }

    char* ucGetControllerId() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getControllerId]);
    }

    void ucGetTCFData() {
        [[UsercentricsUsercentricsUnityCompanion companion] getTCFData];
    }

    char* ucGetUSPData() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getUSPData]);
    }

    void ucRestoreUserSession(const char* controllerId) {
        [[UsercentricsUsercentricsUnityCompanion companion] restoreUserSessionControllerId:UcCreateNSString(controllerId)];
    }

    void ucSubscribeOnConsentUpdated() {
        [[UsercentricsUsercentricsUnityCompanion companion] subscribeOnConsentUpdated];
    }

    void ucDisposeOnConsentUpdatedSubscription() {
        [[UsercentricsUsercentricsUnityCompanion companion] disposeOnConsentUpdatedSubscription];
    }

    void ucSubscribeOnConsentMediation() {
        [[UsercentricsUsercentricsUnityCompanion companion] subscribeOnConsentMediation];
    }

    void ucDisposeOnConsentMediationSubscription() {
        [[UsercentricsUsercentricsUnityCompanion companion] disposeOnConsentMediationSubscription];
    }

    char* ucGetFirstLayerSettings() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getFirstLayerSettings]);
    }

    void ucAcceptAll() {
        [[UsercentricsUsercentricsUnityCompanion companion] acceptAllFirstLayerForTCF];
    }

    void ucDenyAll() {
        [[UsercentricsUsercentricsUnityCompanion companion] denyAllFirstLayerForTCF];
    }

    void ucTrack(int eventType) {
        [[UsercentricsUsercentricsUnityCompanion companion] trackEventTypeEnumIndex:eventType];
    }

    void ucSetCmpId(int cmpId) {
        [[UsercentricsUsercentricsUnityCompanion companion] setCmpIdCmpId:cmpId];
    }

    char* ucGetCmpData() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getCmpData]);
    }

    void ucSetABTestingVariant(char* variant) {
        [[UsercentricsUsercentricsUnityCompanion companion] setABTestingVariantVariantName:UcCreateNSString(variant)];
    }

    char* ucGetABTestingVariant() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getABTestingVariant]);
    }
    
    char* ucGetAdditionalConsentModeData() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getAdditionalConsentModeData]);
    }
    
    char* ucGetConsents() {
        return toChar([[UsercentricsUsercentricsUnityCompanion companion] getConsents]);
    }
    
    void ucClearUserSession() {
        [[UsercentricsUsercentricsUnityCompanion companion] clearUserSession];
    }
}
