#import "PDLConsoleLogger.h"

void _logToConsolePdl(const char *message)
{
    NSLog([NSString stringWithUTF8String:message]);
}
