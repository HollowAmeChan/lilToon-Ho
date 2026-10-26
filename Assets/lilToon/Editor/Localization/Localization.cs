using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace lilToon
{
    internal partial class L10n : ScriptableSingleton<L10n>
    {
        public LocalizationAsset localizationAssetFallback;
        public LocalizationAsset localizationAsset;
        private static string[] languages;
        private static string[] languageNames;
        private static readonly Dictionary<string, GUIContent> guicontents = new();
        private static string localizationFolder => AssetDatabase.GUIDToAssetPath("2feb2bcbf5b4ef043910b310c21b6ba7");

        internal static void Load()
        {
            guicontents.Clear();
            var path = localizationFolder + "/" + ResolveLanguage(Settings.instance.language) + ".po";
            instance.localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path);

            if(!instance.localizationAssetFallback) instance.localizationAssetFallback = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(localizationFolder + "/en-US.po");
            if(!instance.localizationAsset) instance.localizationAsset = new LocalizationAsset();
        }

        internal static string[] GetLanguages()
        {
            if(languages != null) return languages;
            if(string.IsNullOrEmpty(localizationFolder)) return new string[0];
            return languages = AssetDatabase.FindAssets(string.Empty, new[] {localizationFolder})
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".po", System.StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension)
                .Where(f => !f.StartsWith("._"))
                .Distinct().OrderBy(f => f, System.StringComparer.Ordinal).ToArray();
        }

        internal static string ResolveLanguage(string language)
        {
            var available = GetLanguages();
            var exact = available.FirstOrDefault(l => string.Equals(l, language, System.StringComparison.OrdinalIgnoreCase));
            if(exact != null) return exact;
            if(string.IsNullOrEmpty(language)) return "en-US";
            if(language.StartsWith("zh", System.StringComparison.OrdinalIgnoreCase))
            {
                bool traditional = language.IndexOf("Hant", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    language.EndsWith("TW", System.StringComparison.OrdinalIgnoreCase) ||
                    language.EndsWith("HK", System.StringComparison.OrdinalIgnoreCase) ||
                    language.EndsWith("MO", System.StringComparison.OrdinalIgnoreCase);
                var chinese = traditional ? "zh-Hant" : "zh-Hans";
                if(available.Contains(chinese)) return chinese;
            }
            var neutral = language.Split('-')[0];
            return available.FirstOrDefault(l => l.StartsWith(neutral + "-", System.StringComparison.OrdinalIgnoreCase)) ?? "en-US";
        }

        internal static string[] GetLanguageNames()
        {
            return languageNames ??= GetLanguages().Select(l => {
                if(l == "zh-Hans") return "简体中文";
                if(l == "zh-Hant") return "繁體中文";
                return new CultureInfo(l).NativeName;
            }).ToArray();
        }

        internal static string L(string key)
        {
            if (!instance.localizationAsset || !instance.localizationAssetFallback) Load();
            var localized = instance.localizationAsset.GetLocalizedString(key);
            return localized != key ? localized : instance.localizationAssetFallback.GetLocalizedString(key);
        }

        internal static GUIContent G(string key) => G(key, null, "");
        private static GUIContent G(string[] key) => key.Length == 2 ? G(key[0], null, key[1]) : G(key[0], null, null);
        internal static GUIContent G(string key, string tooltip) => G(key, null, tooltip); // From EditorToolboxSettings
        private static GUIContent G(string key, Texture image) => G(key, image, "");

        private static GUIContent G(string key, Texture image, string tooltip)
        {
            if (!instance.localizationAsset || !instance.localizationAssetFallback) Load();
            if (guicontents.TryGetValue(key, out var content)) return content;
            return guicontents[key] = new GUIContent(L(key), image, L(tooltip));
        }
    }
}
