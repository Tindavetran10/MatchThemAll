// App Tracking Transparency bridge for TrackingConsent.cs (iOS only).
#import <AppTrackingTransparency/AppTrackingTransparency.h>

typedef void (*ATTCallback)(int status);

extern "C"
{
    // 0 = not determined, 1 = restricted, 2 = denied, 3 = authorized
    int _ATT_GetStatus()
    {
        if (@available(iOS 14, *))
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        return 3; // before iOS 14 there is no prompt
    }

    void _ATT_RequestPermission(ATTCallback callback)
    {
        if (@available(iOS 14, *))
        {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status)
            {
                dispatch_async(dispatch_get_main_queue(), ^{ if (callback) callback((int)status); });
            }];
        }
        else if (callback)
        {
            callback(3);
        }
    }
}
