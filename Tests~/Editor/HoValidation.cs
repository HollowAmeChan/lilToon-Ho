using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using lilToon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Copy Tests~/Editor and Tests~/Shaders into Assets in an isolated project.
// Run with -batchmode -force-d3d11 -executeMethod LilToonHoValidation.Run.
public static class LilToonHoValidation
{
    private static readonly List<string> results = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static void Check(bool condition, string name)
    {
        if(!condition) throw new Exception(name);
        results.Add(name);
        Debug.Log("[HoValidation] PASS " + name);
    }

    public static void Run()
    {
        try
        {
            Application.logMessageReceived += CaptureError;
            var settings = ScriptableObject.CreateInstance<lilToonSetting>();
            typeof(lilToonSetting).GetMethod("TurnOnAllShaderSetting", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(null, new object[] {settings});
            if(!Environment.GetCommandLineArgs().Contains("-ho-verify-generated"))
                typeof(lilToonSetting).GetMethod("ApplyShaderSetting", BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] {typeof(lilToonSetting), typeof(string)}, null).Invoke(null, new object[] {settings, null});
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Check(settings.LIL_FEATURE_ShadowReceiveMask && settings.LIL_FEATURE_OutlineZBiasMask, "Both texture features enabled");
            CheckGeneratedShaders();
            #if UNITY_2022_1_OR_NEWER
                CheckVariants();
            #endif
            CheckPixels();
            CheckFeatureScanning(settings);
            Check(errors.Count == 0, "No Unity errors during validation: " + string.Join("; ", errors));
            File.WriteAllLines("HoValidation-results.txt", results);
            Debug.Log("[HoValidation] SUCCESS " + results.Count + " checks");
            EditorApplication.Exit(0);
        }
        catch(Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllLines("HoValidation-results.txt", results.Concat(new[] {"FAILED: " + exception}));
            EditorApplication.Exit(1);
        }
    }

    private static void CaptureError(string message, string trace, LogType type)
    {
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
    }

    private static void CheckGeneratedShaders()
    {
        var shaders = new[] {"lilToon", "Hidden/lilToonOutline", "Hidden/lilToonCutoutOutline",
            "Hidden/lilToonTransparentOutline", "Hidden/lilToonLiteOutline", "_lil/lilToonMulti", "Hidden/lilToonMultiOutline"};
        var compile = typeof(ShaderUtil).GetMethod("CompilePass", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] {typeof(Material), typeof(int), typeof(bool)}, null);
        Check(compile != null, "Synchronous shader compilation API available");
        foreach(var name in shaders)
        {
            var shader = Shader.Find(name);
            Check(shader != null, "Find " + name);
            var material = new Material(shader);
            Check(material.HasProperty("_OutlineZBiasMask"), name + " generated Z mask property");
            if(!name.Contains("Lite"))
                Check(material.HasProperty("_ShadowReceiveMask") && !material.HasProperty("_Shadow2ndReceiveMask") &&
                    !material.HasProperty("_Shadow3rdReceiveMask"), name + " exactly one receive mask");
            foreach(var keywords in new[] {new string[0], new[] {"SHADOWS_SCREEN", "_REQUIRE_UV2"}})
            {
                material.shaderKeywords = keywords;
                for(int pass = 0; pass < material.passCount; pass++) compile.Invoke(null, new object[] {material, pass, true});
            }
            var failures = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").ToArray();
            Check(failures.Length == 0, name + " all passes compile: " + string.Join("; ", failures.Select(m => m.message)));
            Object.DestroyImmediate(material);
        }
    }

    private static void CheckVariants()
    {
        var root = new Material(Shader.Find("Hidden/lilToonOutline"));
        root.SetColor("_Color", Color.white);
        const string rootPath = "Assets/HoValidation/root.mat";
        const string childPath = "Assets/HoValidation/child.mat";
        const string nestedPath = "Assets/HoValidation/nested.mat";
        AssetDatabase.CreateAsset(root, rootPath);
        var child = new Material(root);
        var nested = new Material(root);
        AssetDatabase.CreateAsset(child, childPath);
        AssetDatabase.CreateAsset(nested, nestedPath);
        #if UNITY_2022_1_OR_NEWER
        child.parent = root;
        nested.parent = child;
        Check(child.parent == root && nested.parent == child, "Persistent child and nested parent links");
        #endif
        AssetDatabase.SaveAssets();
        var originalShader = root.shader;
        foreach(var material in new[] {child, nested})
        {
            lilToonInspector.SetupMaterialWithRenderingMode(material, RenderingMode.Cutout, TransparentMode.Normal, false, false, false, false);
            Check(material.shader == originalShader && root.shader == originalShader, "Variant retains inherited shader " + (material == child ? "child" : "nested"));
        }
        var inspector = new lilToonInspector();
        inspector.SetMaterials(new[] {root, child});
        var hasVariants = typeof(lilToonInspector).GetMethod("HasMaterialVariants", BindingFlags.Instance | BindingFlags.NonPublic);
        Check((bool)hasVariants.Invoke(inspector, new object[] {root}), "Mixed selection detects child even when standalone is first");
        inspector.SetMaterials(new[] {root});
        Check(!(bool)hasVariants.Invoke(inspector, new object[] {root}), "Standalone selection permits shader switching");
        var createMulti = typeof(lilToonInspector).GetMethod("CreateMultiMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
        inspector.SetMaterials(new[] {root, child});
        createMulti.Invoke(inspector, new object[] {root});
        Check(root.shader == originalShader, "Mixed selection cannot convert parent to Multi");
        child.SetColor("_Color", Color.red);
        child.SetFloat("_OutlineWidth", 0.3f);
        child.SetTexture("_OutlineZBiasMask", Texture2D.blackTexture);
        Check(child.GetColor("_Color") == Color.red && root.GetColor("_Color") == Color.white, "Variant color override leaves parent unchanged");
        Check(Mathf.Approximately(child.GetFloat("_OutlineWidth"), 0.3f) &&
            child.GetTexture("_OutlineZBiasMask") == Texture2D.blackTexture, "Variant numeric and mask overrides remain editable");
        lilToonInspector.SetupMaterialWithRenderingMode(root, RenderingMode.Cutout, TransparentMode.Normal, true, false, false, false);
        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(rootPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(childPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(nestedPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        Check(root.shader == Shader.Find("Hidden/lilToonCutoutOutline"), "Standalone can switch rendering mode");
        Check(child.shader == root.shader && nested.shader == root.shader, "Children inherit parent shader changes");
        AssetDatabase.DeleteAsset(nestedPath); AssetDatabase.DeleteAsset(childPath); AssetDatabase.DeleteAsset(rootPath);
    }

    private static Color Pixel(Material material)
    {
        var target = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        var previous = RenderTexture.active;
        Graphics.Blit(Texture2D.whiteTexture, target, material);
        RenderTexture.active = target;
        var readback = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
        readback.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
        var color = readback.GetPixel(4, 4);
        Object.DestroyImmediate(readback);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
        return color;
    }

    private static void Near(float actual, float expected, string name)
    {
        Check(Mathf.Abs(actual - expected) < 0.025f, name + " actual=" + actual + " expected=" + expected);
    }

    private static bool Different(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) > 0.025f || Mathf.Abs(a.g - b.g) > 0.025f || Mathf.Abs(a.b - b.b) > 0.025f;
    }

    private static void NearColor(Color actual, Color expected, string name)
    {
        Near(actual.r, expected.r, name + " R");
        Near(actual.g, expected.g, name + " G");
        Near(actual.b, expected.b, name + " B");
    }

    private static Texture2D MakeMatCapGradient(bool green)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
        for(int y = 0; y < size; y++)
        {
            for(int x = 0; x < size; x++)
            {
                float value = (green ? y : x) / (size - 1.0f);
                texture.SetPixel(x, y, green ? new Color(0, value, 0, 1) : new Color(value, 0, 0, 1));
            }
        }
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D MakeNormalTexture(Color encoded)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        texture.SetPixel(0, 0, encoded);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false, false);
        return texture;
    }

    private static void ConfigureMatCapFixture(Material material, Texture2D matCap, Texture2D matCap2nd,
        Texture2D customNormal, Texture2D tangentMap, Texture2D scaleMask)
    {
        material.SetFloat("_TestMode", 3);
        material.SetTexture("_MatCapTex", matCap);
        material.SetTexture("_MatCap2ndTex", matCap2nd);
        material.SetTexture("_MatCapBumpMap", customNormal);
        material.SetTexture("_MatCap2ndBumpMap", customNormal);
        material.SetTexture("_AnisotropyTangentMap", tangentMap);
        material.SetTexture("_AnisotropyScaleMask", scaleMask);
        material.SetVector("_MatCapTex_ST", new Vector4(1, 1, 0, 0));
        material.SetVector("_MatCap2ndTex_ST", new Vector4(1, 1, 0, 0));
        material.SetVector("_MatCapBumpMap_ST", new Vector4(1, 1, 0, 0));
        material.SetVector("_MatCap2ndBumpMap_ST", new Vector4(1, 1, 0, 0));
        material.SetVector("_AnisotropyTangentMap_ST", new Vector4(1, 1, 0, 0));
        material.SetVector("_AnisotropyScaleMask_ST", new Vector4(1, 1, 0, 0));
        material.SetVector("_MatCapBlendUV1", Vector4.zero);
        material.SetVector("_MatCap2ndBlendUV1", Vector4.zero);
        material.SetColor("_MatCapColor", Color.white);
        material.SetColor("_MatCap2ndColor", Color.white);
        material.SetFloat("_MatCapBlend", 1);
        material.SetFloat("_MatCap2ndBlend", 1);
        material.SetFloat("_MatCapBlendMode", 1);
        material.SetFloat("_MatCap2ndBlendMode", 1);
        material.SetFloat("_MatCapEnableLighting", 0);
        material.SetFloat("_MatCap2ndEnableLighting", 0);
        material.SetFloat("_MatCapShadowMask", 0);
        material.SetFloat("_MatCap2ndShadowMask", 0);
        material.SetFloat("_MatCapBackfaceMask", 0);
        material.SetFloat("_MatCap2ndBackfaceMask", 0);
        material.SetFloat("_MatCapLod", 0);
        material.SetFloat("_MatCap2ndLod", 0);
        material.SetFloat("_MatCapMainStrength", 0);
        material.SetFloat("_MatCap2ndMainStrength", 0);
        material.SetFloat("_MatCapNormalStrength", 1);
        material.SetFloat("_MatCap2ndNormalStrength", 1);
        material.SetFloat("_MatCapBumpScale", 1);
        material.SetFloat("_MatCap2ndBumpScale", 1);
        material.SetFloat("_MatCapPerspective", 0);
        material.SetFloat("_MatCap2ndPerspective", 0);
        material.SetFloat("_MatCapZRotCancel", 1);
        material.SetFloat("_MatCap2ndZRotCancel", 1);
        material.SetFloat("_MatCapCustomNormal", 1);
        material.SetFloat("_MatCap2ndCustomNormal", 1);
        material.SetFloat("_UseMatCap", 1);
        material.SetFloat("_UseMatCap2nd", 1);
        material.SetFloat("_UseAnisotropy", 1);
        material.SetFloat("_AnisotropyScale", -0.8f);
        material.SetFloat("_Anisotropy2MatCap", 0);
        material.SetFloat("_Anisotropy2MatCap2nd", 0);
        material.SetVector("_TestMainNormal", new Vector4(0, 0, 1, 0));
        material.SetFloat("_TestFacing", 1);
        material.SetFloat("_FlipNormal", 1);
    }

    private static void CheckMatCapAnisotropy()
    {
        var material = new Material(Shader.Find("Hidden/lilToonHoValidation"));
        var matCap = MakeMatCapGradient(false);
        var matCap2nd = MakeMatCapGradient(true);
        // DXT5nm decoding uses alpha *= red, so this gives a non-axis-aligned test normal.
        var customNormal = MakeNormalTexture(new Color(0.5f, 0.8f, 1, 1));
        var tangentMap = MakeNormalTexture(new Color(0.65f, 0.5f, 1, 1));
        var white = MakeNormalTexture(Color.white);
        var black = MakeNormalTexture(Color.black);
        ConfigureMatCapFixture(material, matCap, matCap2nd, customNormal, tangentMap, white);

        foreach(int layer in new[] {1, 2})
        {
            string custom = layer == 1 ? "_MatCapCustomNormal" : "_MatCap2ndCustomNormal";
            string effect = layer == 1 ? "_Anisotropy2MatCap" : "_Anisotropy2MatCap2nd";
            string otherCustom = layer == 1 ? "_MatCap2ndCustomNormal" : "_MatCapCustomNormal";
            string otherEffect = layer == 1 ? "_Anisotropy2MatCap2nd" : "_Anisotropy2MatCap";
            material.SetFloat("_TestMode", layer == 1 ? 4 : 5);
            material.SetFloat(custom, 1);
            material.SetFloat(effect, 0);
            Color initialNormal = Pixel(material);
            NearColor(initialNormal, new Color(0.5f, 0.5f, 1, 1), "MatCap initial normal layer " + layer);
            material.SetFloat(effect, 1);
            NearColor(Pixel(material), initialNormal, "Custom MatCap blocks anisotropy distribution layer " + layer);
            material.SetFloat(otherCustom, 0);
            material.SetFloat(otherEffect, 1);
            NearColor(Pixel(material), initialNormal, "Other non-custom layer does not bypass custom priority layer " + layer);
            material.SetFloat(custom, 0);
            Check(Different(Pixel(material), initialNormal), "Non-custom MatCap receives anisotropy distribution layer " + layer);
            material.SetFloat(effect, 0);
            NearColor(Pixel(material), initialNormal, "Anisotropy effect switch restores initial normal layer " + layer);
            material.SetFloat(otherCustom, 1);
            material.SetFloat(otherEffect, 0);
        }
        ConfigureMatCapFixture(material, matCap, matCap2nd, customNormal, tangentMap, white);

        Color customBaseline = Pixel(material);
        material.SetFloat("_Anisotropy2MatCap", 1);
        Color customFirstAnisotropy = Pixel(material);
        material.SetFloat("_Anisotropy2MatCap", 0);
        material.SetFloat("_Anisotropy2MatCap2nd", 1);
        Color customSecondAnisotropy = Pixel(material);
        NearColor(customFirstAnisotropy, customBaseline, "Custom first MatCap ignores anisotropy");
        NearColor(customSecondAnisotropy, customBaseline, "Custom second MatCap ignores anisotropy");
        material.SetFloat("_Anisotropy2MatCap", 1);
        material.SetFloat("_Anisotropy2MatCap2nd", 1);
        material.SetFloat("_AnisotropyScale", 0.8f);
        NearColor(Pixel(material), customBaseline, "Custom MatCap ignores anisotropy scale");

        material.SetTexture("_AnisotropyScaleMask", black);
        NearColor(Pixel(material), customBaseline, "Zero anisotropy mask returns custom-normal baseline");
        material.SetTexture("_AnisotropyScaleMask", white);

        material.SetFloat("_MatCapCustomNormal", 1);
        material.SetFloat("_MatCap2ndCustomNormal", 1);
        material.SetVector("_TestMainNormal", new Vector4(0, 0, 1, 0));
        Color mainNormalA = Pixel(material);
        material.SetVector("_TestMainNormal", new Vector4(0.5f, 0, 0.8660254f, 0));
        Color mainNormalB = Pixel(material);
        NearColor(mainNormalB, mainNormalA, "Main normal does not pollute custom MatCap");

        material.SetFloat("_MatCapCustomNormal", 0);
        material.SetFloat("_MatCap2ndCustomNormal", 0);
        material.SetVector("_TestMainNormal", new Vector4(0, 0, 1, 0));
        material.SetFloat("_AnisotropyScale", -0.8f);
        material.SetFloat("_Anisotropy2MatCap", 0);
        material.SetFloat("_Anisotropy2MatCap2nd", 0);
        Color nonCustomBaseline = Pixel(material);
        material.SetFloat("_Anisotropy2MatCap", 1);
        Color nonCustomFirstAnisotropy = Pixel(material);
        material.SetFloat("_Anisotropy2MatCap", 0);
        material.SetFloat("_Anisotropy2MatCap2nd", 1);
        Color nonCustomSecondAnisotropy = Pixel(material);
        Check(Different(nonCustomFirstAnisotropy, nonCustomBaseline), "Non-custom first MatCap uses anisotropy");
        Check(Different(nonCustomSecondAnisotropy, nonCustomBaseline), "Non-custom second MatCap uses anisotropy");
        Near(nonCustomFirstAnisotropy.g, nonCustomBaseline.g, "Non-custom first anisotropy leaves second-layer channel independent");
        Near(nonCustomSecondAnisotropy.r, nonCustomBaseline.r, "Non-custom second anisotropy leaves first-layer channel independent");
        material.SetFloat("_Anisotropy2MatCap", 1);
        material.SetFloat("_Anisotropy2MatCap2nd", 1);
        material.SetTexture("_AnisotropyScaleMask", black);
        NearColor(Pixel(material), nonCustomBaseline, "Zero anisotropy mask returns non-custom baseline");
        material.SetTexture("_AnisotropyScaleMask", white);

        material.SetFloat("_MatCapCustomNormal", 1);
        material.SetFloat("_MatCap2ndCustomNormal", 1);
        material.SetVector("_TestMainNormal", new Vector4(0, 0, 1, 0));
        material.SetFloat("_Anisotropy2MatCap", 0);
        material.SetFloat("_Anisotropy2MatCap2nd", 0);
        material.SetFloat("_TestFacing", 1);
        Color front = Pixel(material);
        material.SetFloat("_TestFacing", -1);
        Color back = Pixel(material);
        Check(IsFinite(front) && IsFinite(back), "Custom MatCap normals remain finite on both faces");
        Check(Different(front, back), "Custom MatCap normal flips once on the back face");
        material.SetFloat("_Anisotropy2MatCap", 1);
        material.SetFloat("_Anisotropy2MatCap2nd", 1);
        material.SetFloat("_TestFacing", 1);
        Color anisotropicFront = Pixel(material);
        material.SetFloat("_TestFacing", -1);
        Color anisotropicBack = Pixel(material);
        Check(IsFinite(anisotropicFront) && IsFinite(anisotropicBack), "Custom MatCap normals remain finite with anisotropy on both faces");
        Check(Different(anisotropicFront, anisotropicBack), "Custom MatCap normal flips once with anisotropy on the back face");

        Object.DestroyImmediate(material);
        Object.DestroyImmediate(matCap);
        Object.DestroyImmediate(matCap2nd);
        Object.DestroyImmediate(customNormal);
        Object.DestroyImmediate(tangentMap);
        Object.DestroyImmediate(white);
        Object.DestroyImmediate(black);
    }

    private static bool IsFinite(Color color)
    {
        return !float.IsNaN(color.r) && !float.IsInfinity(color.r) &&
            !float.IsNaN(color.g) && !float.IsInfinity(color.g) &&
            !float.IsNaN(color.b) && !float.IsInfinity(color.b);
    }

    private static void CheckPixels()
    {
        var material = new Material(Shader.Find("Hidden/lilToonHoValidation"));
        foreach(var input in new[] {Color.white, Color.black, new Color(0.25f,0.75f,0.5f,1)})
        {
            material.SetColor("_TestColor", input);
            var color = Pixel(material);
            bool fallback = input == Color.white || input == Color.black;
            Near(color.r, fallback ? 0.5f : input.r, "Outline vector R " + input);
            Near(color.g, fallback ? 0.5f : input.g, "Outline vector G " + input);
            Near(color.b, fallback ? 1 : input.b, "Outline vector B " + input);
        }
        var gray = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        gray.SetPixel(0, 0, new Color(0.5f,0.5f,0.5f,1)); gray.Apply();
        material.SetFloat("_TestMode", 1);
        foreach(var mask in new[] {Texture2D.whiteTexture, Texture2D.blackTexture, gray})
        {
            material.SetTexture("_OutlineZBiasMask", mask);
            Near(Pixel(material).r, mask == gray ? 0.4f : mask == Texture2D.whiteTexture ? 0.8f : 0, "Vertex-stage Z bias mask " + mask.name);
        }
        material.SetFloat("_OutlineZBias", -0.8f);
        Near(Pixel(material).r, -0.4f, "Negative Z bias is masked");
        material.EnableKeyword("HO_TEST_STRIPPED");
        Near(Pixel(material).r, -0.8f, "Stripped Z mask preserves scalar bias");
        material.DisableKeyword("HO_TEST_STRIPPED");
        material.SetFloat("_TestMode", 2);
        foreach(int layer in new[] {1,2,3})
        {
            material.SetFloat("_Shadow2ndReceive", layer >= 2 ? 1 : 0);
            material.SetFloat("_Shadow3rdReceive", layer >= 3 ? 1 : 0);
            material.SetColor("_Shadow2ndColor", new Color(0.4f,0.4f,0.4f,layer >= 2 ? 1 : 0));
            material.SetColor("_Shadow3rdColor", new Color(0.2f,0.2f,0.2f,layer >= 3 ? 1 : 0));
            material.SetTexture("_ShadowReceiveMask", Texture2D.blackTexture);
            Near(Pixel(material).r, 1, "Shared black mask rejects received shadow layer " + layer);
            material.SetTexture("_ShadowReceiveMask", Texture2D.whiteTexture);
            Near(Pixel(material).r, layer == 1 ? 0.8f : layer == 2 ? 0.4f : 0.2f, "Shared white mask receives shadow layer " + layer);
        }
        material.SetFloat("_ShadowMaskType", 1);
        material.SetTexture("_ShadowStrengthMask", Texture2D.blackTexture);
        material.SetTexture("_ShadowReceiveMask", Texture2D.blackTexture);
        Near(Pixel(material).r, 1, "Flat shadow path respects black receive mask");
        material.SetTexture("_ShadowReceiveMask", Texture2D.whiteTexture);
        Check(Pixel(material).r < 0.3f, "Flat shadow path receives shadow with white mask");
        material.SetTexture("_ShadowReceiveMask", Texture2D.blackTexture);
        material.EnableKeyword("HO_TEST_STRIPPED");
        Check(Pixel(material).r < 0.3f, "Stripped receive mask preserves original receive strength");
        var failures = ShaderUtil.GetShaderMessages(material.shader).Where(m => m.severity.ToString() == "Error").ToArray();
        Check(failures.Length == 0, "GPU fixture shader has no compile errors");
        Object.DestroyImmediate(material); Object.DestroyImmediate(gray);
        CheckMatCapAnisotropy();
    }

    private static void CheckFeatureScanning(lilToonSetting settings)
    {
        var material = new Material(Shader.Find("Hidden/lilToonOutline"));
        material.SetTexture("_ShadowReceiveMask", Texture2D.blackTexture);
        material.SetTexture("_OutlineZBiasMask", Texture2D.blackTexture);
        settings.LIL_FEATURE_ShadowReceiveMask = false;
        settings.LIL_FEATURE_OutlineZBiasMask = false;
        var check = typeof(lilToonSetting).GetMethod("CheckTextures", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] {typeof(lilToonSetting).MakeByRefType(), typeof(Material)}, null);
        check.Invoke(null, new object[] {settings, material});
        Check(settings.LIL_FEATURE_ShadowReceiveMask && settings.LIL_FEATURE_OutlineZBiasMask, "Assigned masks survive feature scanning");
        Object.DestroyImmediate(material);
    }
}
