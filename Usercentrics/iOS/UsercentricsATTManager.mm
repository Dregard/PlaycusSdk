#import <Foundation/Foundation.h>
#import "TrackingTransparencyManager.h"

extern "C" {

    NSUInteger ucInterfaceGetTrackingAuthorizationStatus() {
        if (@available(iOS 14, *)) {
            return [[TrackingTransparencyManager sharedInstance] getTrackingAuthorizationStatus];
        } else {
            return 0;
        }
    }

    void ucRequestForAppTrackingTransparency(TrackingTransparencyDelegate delegate) {
        if (@available(iOS 14, *)) {
            [[TrackingTransparencyManager sharedInstance] trackingAuthorizationRequest:delegate];
        } else {
            delegate(3);
        }
    }
}
