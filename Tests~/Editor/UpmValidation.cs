using System;
using System.IO;
using System.Linq;
using System.Reflection;
using lilToon;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

// Run in an isolated project with lilToon installed through UPM (local or Git).
public static class LilToonHoUpmValidation
{
    private static void Check(bool condition, string name)
    {
        if(!condition) throw new Exception(name);
        Debug.Log("[HoUPM] PASS " + name);
    }

    public static void Run()
    {
        try
        {
            const string root = "Packages/jp.lilxyzw.liltoon";
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(root + "/package.json");
            Check(package != null && (package.source == PackageSource.Local || package.source == PackageSource.Git), "UPM local/Git package resolved");
            Debug.Log("[HoUPM] Source=" + package.source + " path=" + package.resolvedPath);
            var assembly = typeof(lilToonInspector).Assembly;
            var directory = assembly.GetType("lilToon.lilDirectoryManager");
            var resolvePath = directory.GetMethod("GetFullPath", BindingFlags.Public | BindingFlags.Static);
            var physical = (string)resolvePath.Invoke(null, new object[] {root + "/package.json"});
            Check(File.Exists(physical) && physical.StartsWith(package.resolvedPath, StringComparison.OrdinalIgnoreCase), "Package virtual path resolves to registered physical root");
            var assetsPath = (string)resolvePath.Invoke(null, new object[] {"Assets/HoValidation"});
            Check(assetsPath == Path.GetFullPath("Assets/HoValidation"), "Assets paths retain normal filesystem meaning");
            Check((string)resolvePath.Invoke(null, new object[] {physical}) == physical, "Absolute paths pass through unchanged");
            Check(AssetDatabase.GUIDToAssetPath("df12117ecd77c31469c224178886498e").StartsWith(root + "/", StringComparison.Ordinal), "Shader GUID resolves inside UPM package");
            Check(lilConstants.currentVersionName == "2.4.1", "Package version discovered through UPM");
            var l10n = assembly.GetType("lilToon.L10n");
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var names = (string[])l10n.GetMethod("GetLanguageNames", flags).Invoke(null, null);
            var languages = (string[])l10n.GetMethod("GetLanguages", flags).Invoke(null, null);
            Check(languages.Length == 5 && names.Length == languages.Length && languages.Contains("zh-Hans"), "All five package languages enumerated before Inspector access");
            var resolveLanguage = l10n.GetMethod("ResolveLanguage", flags);
            Check((string)resolveLanguage.Invoke(null, new object[] {"zh-CN"}) == "zh-Hans" &&
                (string)resolveLanguage.Invoke(null, new object[] {"zh-TW"}) == "zh-Hant" &&
                (string)resolveLanguage.Invoke(null, new object[] {"ja"}) == "ja-JP", "Regional and neutral culture names resolve to existing translations");
            Check(lilLanguageManager.GetLoc("sShadowReceiveMask") != "sShadowReceiveMask", "New Inspector label loads from package translation assets");
            // The main harness regenerates shaders, compiles all selected passes,
            // tests variants and GPU output, then exits Unity with the result.
            LilToonHoValidation.Run();
        }
        catch(Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
