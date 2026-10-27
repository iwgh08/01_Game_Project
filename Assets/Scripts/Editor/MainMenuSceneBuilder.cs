using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 타이틀 화면 씬을 만든다
public static class MainMenuSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";

    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.fog = false;

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f;
        camGo.AddComponent<AudioListener>();
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = BuilderKit.HorrorVolumeProfile();

        var ambience = new GameObject("Ambience").AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.volume = 0.5f;

        var gothic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameBuildMenu.GothicFontPath);
        var pen = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameBuildMenu.PenFontPath);
        UIBuilder.BuildMainMenuUI(gothic, pen, cam, ambience);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
    }
}
