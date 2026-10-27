using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// 메뉴: Tools > 방과후 방송 > 전체 다시 만들기
// 한글 폰트 에셋(없을 때만) → 타이틀 씬 → 데이터 에셋과 학교 씬을 새로 만들고 Build Profiles 씬 목록을 맞춘다.
// 주의: 두 씬을 덮어쓰므로, 씬을 에디터에서 직접 고쳤다면 그 변경은 사라진다.
public static class GameBuildMenu
{
    public const string GothicFontPath = "Assets/Fonts/NanumGothic-Regular SDF.asset";
    public const string PenFontPath = "Assets/Fonts/NanumPenScript-Regular SDF.asset";

    [MenuItem("Tools/방과후 방송/전체 다시 만들기")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFonts();
        BuilderKit.Begin();
        MainMenuSceneBuilder.Build();
        SchoolSceneBuilder.Build();
        WarmFonts();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuSceneBuilder.ScenePath, true),
            new EditorBuildSettingsScene(SchoolSceneBuilder.ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[방과후 방송] 씬 생성 완료: " + SchoolSceneBuilder.ScenePath);
    }

    // TTF에서 동적 SDF 폰트 에셋을 만들고 나눔고딕을 TMP 기본 폰트로 지정한다.
    // 아틀라스는 텍스트(YAML)로 저장되므로 저장소가 커지지 않게 48pt / 1024px로 만든다.
    static void EnsureFonts()
    {
        var gothic = CreateFontAsset(GothicFontPath, "Assets/Fonts/NanumGothic-Regular.ttf");
        CreateFontAsset(PenFontPath, "Assets/Fonts/NanumPenScript-Regular.ttf");

        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = gothic;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static TMP_FontAsset CreateFontAsset(string assetPath, string ttfPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        var fontAsset = TMP_FontAsset.CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        string baseName = System.IO.Path.GetFileNameWithoutExtension(ttfPath);
        fontAsset.name = baseName + " SDF";
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        fontAsset.atlasTextures[0].name = baseName + " Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        fontAsset.material.name = baseName + " Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    // 씬과 문서에 쓰인 글자를 폰트 아틀라스에 미리 넣어 둔다 (처음 표시할 때 끊김 방지)
    static void WarmFonts()
    {
        string text = BuilderKit.UsedText.ToString();
        foreach (string path in new[] { GothicFontPath, PenFontPath })
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            font.TryAddCharacters(text, out _);
            EditorUtility.SetDirty(font);
        }
    }
}
