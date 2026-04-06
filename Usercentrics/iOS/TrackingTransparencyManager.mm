#import <AdSupport/AdSupport.h>
#import <Foundation/Foundation.h>
#import <dlfcn.h>
#import <objc/runtime.h>
#import <sys/utsname.h>
#import "TrackingTransparencyManager.h"

@interface TrackingTransparencyManager ()
@property (strong, nonatomic) Class trackingManagerAuthorizationClass;
@end

@implementation TrackingTransparencyManager
+ (TrackingTransparencyManager *)sharedInstance {
    static TrackingTransparencyManager *instance = nil;
    static dispatch_once_t token;
    dispatch_once(&token, ^{
      instance = [[TrackingTransparencyManager alloc] init];
    });
    return instance;
}

- (instancetype)init {
    if (self = [super init]) {
        if (![TrackingTransparencyManager loadFramework]) {
            NSLog(@"Can't load ATTrackingManager");
        }
        self.trackingManagerAuthorizationClass = NSClassFromString(@"ATTrackingManager");
    }
    return self;
}

- (BOOL)isAvailable {
    return self.trackingManagerAuthorizationClass != nil && [[NSBundle mainBundle] objectForInfoDictionaryKey:@"NSUserTrackingUsageDescription"] != nil;
}

- (void)trackingAuthorizationRequest: (TrackingTransparencyDelegate) delegate {
    if (!self.isAvailable)
        return;
    id handler = ^(NSUInteger result) {
        delegate((unsigned long)result);
    };

    SEL requestSelector = NSSelectorFromString(@"requestTrackingAuthorizationWithCompletionHandler:");
    if ([self.trackingManagerAuthorizationClass respondsToSelector:requestSelector]) {
        [self.trackingManagerAuthorizationClass performSelector:requestSelector withObject:handler];
    }
}

- (NSUInteger)getTrackingAuthorizationStatus {
    if (!self.isAvailable)
        return 0;
    NSUInteger value = [[self.trackingManagerAuthorizationClass valueForKey:@"trackingAuthorizationStatus"] unsignedIntegerValue];
    return value;
}

+ (BOOL)isFrameworkPresent {
    id attClass = objc_getClass("ATTrackingManager");
    if (attClass)
        return TRUE;
    return FALSE;
}

+ (BOOL)isDeviceSimulator {
    struct utsname systemInfo;
    uname(&systemInfo);
    NSString *model = [NSString stringWithCString:systemInfo.machine encoding:NSUTF8StringEncoding];
    if ([model isEqualToString:@"x86_64"] || [model isEqualToString:@"i386"])
        return TRUE;
    return FALSE;
}

+ (BOOL)loadFramework {
    NSString *frameworkLocation;
    if (![TrackingTransparencyManager isFrameworkPresent]) {
        NSLog(@"AppTrackingTransparency Framework is not present, trying to load it.");
        if ([TrackingTransparencyManager isDeviceSimulator]) {
            NSString *frameworkPath = [[NSProcessInfo processInfo] environment]
                [@"DYLD_FALLBACK_FRAMEWORK_PATH"];
            if (frameworkPath) {
                frameworkLocation = [NSString pathWithComponents:@[ frameworkPath, @"AppTrackingTransparency.framework", @"AppTrackingTransparency" ]];
            }
        } else {
            frameworkLocation = [NSString stringWithFormat:@"/System/Library/Frameworks/AppTrackingTransparency.framework/AppTrackingTransparency"];
        }
        dlopen([frameworkLocation cStringUsingEncoding:NSUTF8StringEncoding], RTLD_LAZY);
        if (![TrackingTransparencyManager isFrameworkPresent]) {
            NSLog(@"AppTrackingTransparency still not present!");
            return FALSE;
        } else {
            NSLog(@"Successfully loaded AppTrackingTransparency framework");
            return TRUE;
        }
    } else {
        NSLog(@"AppTrackingTransparency framework already present");
        return TRUE;
    }
}

@end
