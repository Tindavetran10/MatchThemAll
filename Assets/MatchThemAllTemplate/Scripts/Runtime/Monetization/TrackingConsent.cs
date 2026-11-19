using System;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace MatchThemAll.Scripts.Monetization
{
    /// <summary>
    /// iOS App Tracking Transparency. Ask once, before the ad SDK starts (Apple requirement on iOS 14.5+).
    /// Native side: Plugins/iOS/ATTRequester.mm. Elsewhere (Android, Editor) it completes immediately.
    /// </summary>
    public static class TrackingConsent
    {
#if UNITY_IOS && !UNITY_EDITOR
        private delegate void StatusCallback(int status);

        [DllImport("__Internal")] private static extern int _ATT_GetStatus();
        [DllImport("__Internal")] private static extern void _ATT_RequestPermission(StatusCallback callback);

        private const int NotDetermined = 0;
        private static Action _pending;

        [AOT.MonoPInvokeCallback(typeof(StatusCallback))]
        private static void OnStatus(int status)
        {
            var done = _pending;
            _pending = null;
            done?.Invoke();
        }

        public static void Request(Action onDone)
        {
            if (_ATT_GetStatus() != NotDetermined) { onDone?.Invoke(); return; } // already answered
            _pending = onDone;
            _ATT_RequestPermission(OnStatus);
        }
#else
        public static void Request(Action onDone) => onDone?.Invoke();
#endif
    }
}
