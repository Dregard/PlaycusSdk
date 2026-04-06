#import <Foundation/Foundation.h>

@interface NSString (PDLHelpers)

+ (BOOL) stringIsNilOrEmpty: (NSString*) aString;

+ (NSString *) jsonStringWithContentsOfDictionary: (NSDictionary *) aDictionary;

- (NSString *) md5;

@end
