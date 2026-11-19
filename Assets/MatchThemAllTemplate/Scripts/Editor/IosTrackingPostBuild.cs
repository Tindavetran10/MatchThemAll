#if UNITY_IOS
using System.IO;
using MatchThemAll.Scripts.Monetization;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Writes NSUserTrackingUsageDescription (the text of the iOS tracking prompt) into the built Info.plist.
/// Apple rejects builds that show the App Tracking Transparency prompt without it.
/// </summary>
public static class IosTrackingPostBuild
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;

        var settings = Resources.Load<MonetizationSettingsSO>(MonetizationSettingsSO.ResourcePath);
        string text = settings && !string.IsNullOrWhiteSpace(settings.iosTrackingUsageDescription)
            ? settings.iosTrackingUsageDescription
            : "Your data will be used to show you more relevant ads and to keep the game free.";

        string plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetString("NSUserTrackingUsageDescription", text);
        plist.WriteToFile(plistPath);
    }
}
#endif
