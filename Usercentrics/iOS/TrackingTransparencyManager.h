#import <Foundation/Foundation.h>

@interface TrackingTransparencyManager : NSObject

+ (TrackingTransparencyManager *)sharedInstance;

typedef void (*TrackingTransparencyDelegate)(NSUInteger status);

- (BOOL)isAvailable;
- (void)trackingAuthorizationRequest: (TrackingTransparencyDelegate) delegate;
- (NSUInteger)getTrackingAuthorizationStatus;

@end
