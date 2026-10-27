using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static BuilderKit;

// 학교 씬 전체를 기본 도형으로 만든다. 실행할 때마다 씬을 처음부터 새로 만든다.
// 좌표: x = 동쪽(복도 방향), z = 북쪽(교실 창문 방향), 단위 m. 바닥 y=0, 천장 y=3.2
//   로비 x -6~0 / 복도 x 0~28, z 0~3 / 교실 x 2~11, z 3~11 / 음악실 x 12~21 / 방송실 x 23~28, z 3~8
public static class SchoolSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/School.unity";
    const float H = 3.2f, T = 0.2f, Wainscot = 1f;
    // 형광등 밝기 일괄 배율 (개별 값은 CeilingLight 호출에서 정한다)
    const float CeilingLightBoost = 1.4f;

    static GameContent content;
    static TMP_FontAsset gothic, pen;
    static Material gothicLit, penLit;

    static Material wallUpper, wallLower, terrazzo, woodFloor, ceilingMat, chalkboard, deskWood, darkWood, metal, lockerMetal, lockerDoorMat,
        paper, photoMat, black, keyWhite, tube, exitGreen, lamp, figure, ground, cork, plastic, plasticDark, glass, frame, redButton,
        brass, diaryCover, bookCover, clockFace, fuseMat, doorWood, doorMetal, signPlate, darkGlass, extinguisher;

    static readonly Color Chalk = new(0.86f, 0.86f, 0.8f);
    static readonly Color Ink = new(0.12f, 0.1f, 0.09f);

    // 이벤트 연결에 쓰는 참조
    static Flashlight playerFlashlight;
    static SubtitleUI subtitles;
    static EndingSequence ending;
    static Door frontDoor, lockerDoor;
    static Drawer pianoDrawer;
    static CombinationLock lockerLock;
    static GameObject padlock, boardWriting, silhouette;
    static PickupItem flashlightPickup;
    static ReadableNote diaryNote;
    static PianoPuzzle piano;
    static FuseBox fuseBox;
    static BroadcastConsole console;
    static FlickerLight lobbyLight, broadcastLight;
    static FlickerLight[] corridorLights;
    static TriggerZone introTrigger, endingTrigger, flickerTrigger, chairDragTrigger, classroomEntry, classroomReentry, musicEntry, broadcastEntry;
    static SoundEmitter chairDragSound, stingerSound, staticBreathSound;
    static AudioSource lobbySpeaker, classroomSpeaker, musicSpeaker, monitorSpeaker;
    static AudioSource[] corridorSpeakers;

    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // 새 씬을 열면 Unity가 안 쓰는 에셋을 메모리에서 내리므로, 데이터 에셋은 씬을 연 다음에 불러와야 참조가 살아 있다
        content = GameContentBuilder.Build();

        LoadFonts();
        CreateMaterials();
        SetupEnvironment();

        var world = Group("World", null);
        BuildStructure(world);
        var player = BuildPlayer(new Vector3(-5.2f, 0f, 1.5f), 90f);
        var ui = UIBuilder.BuildGameUI(gothic, pen, player.GetComponent<Inventory>());
        subtitles = ui.subtitles;
        BuildGameManager(ui);

        BuildLobby(world);
        BuildCorridor(world);
        BuildClassroom(world);
        BuildMusicRoom(world);
        BuildBroadcastRoom(world);
        WireEvents();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
    }

    // ───────── 준비 ─────────

    static void LoadFonts()
    {
        gothic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameBuildMenu.GothicFontPath);
        pen = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameBuildMenu.PenFontPath);
        // 손글씨 폰트에 없는 기호(♪ ‖ ♥ →)는 나눔고딕으로 대신 그린다
        pen.fallbackFontAssetTable ??= new System.Collections.Generic.List<TMP_FontAsset>();
        if (!pen.fallbackFontAssetTable.Contains(gothic))
        {
            pen.fallbackFontAssetTable.Add(gothic);
            EditorUtility.SetDirty(pen);
        }
        gothicLit = LitTextMaterial(gothic, "Text_Gothic_Lit");
        penLit = LitTextMaterial(pen, "Text_Pen_Lit");
    }

    static void CreateMaterials()
    {
        var grime = Texture("Grime", 512, (u, v) =>
        {
            float n = TileNoise(u, v, 4, 4, 100) * 0.5f + TileNoise(u, v, 9, 9, 200) * 0.3f + TileNoise(u, v, 23, 23, 300) * 0.2f;
            float stain = Mathf.SmoothStep(0.62f, 0.75f, TileNoise(u, v, 3, 3, 400));
            return 0.8f + (n - 0.5f) * 0.4f - stain * 0.2f;
        });
        var tiles = Texture("FloorTiles", 512, (u, v) =>
        {
            float speck = Hash(u * 512f, v * 512f) > 0.93f ? 0.22f : 0f;
            float grout = Frac(u * 2f) < 0.012f || Frac(v * 2f) < 0.012f ? 0.45f : 1f;
            return (0.75f + (TileNoise(u, v, 6, 6, 500) - 0.5f) * 0.25f - speck) * grout;
        });
        var planks = Texture("WoodPlanks", 512, (u, v) =>
        {
            int plank = Mathf.FloorToInt(v * 6f);
            float seam = Frac(v * 6f) < 0.03f || Frac(u + Hash(plank, 3f)) < 0.006f ? 0.5f : 1f;
            float tone = 0.78f + (Hash(plank, 7f) - 0.5f) * 0.25f;
            return (tone + (TileNoise(u, v, 3, 60, 600) - 0.5f) * 0.35f) * seam;
        });

        wallUpper = Mat("WallUpper", new Color(0.62f, 0.6f, 0.53f), 0.1f, 0f, grime);
        wallLower = Mat("WallLower", new Color(0.24f, 0.33f, 0.28f), 0.25f, 0f, grime);
        terrazzo = Mat("FloorTerrazzo", new Color(0.42f, 0.42f, 0.4f), 0.35f, 0f, tiles);
        woodFloor = Mat("FloorWood", new Color(0.36f, 0.25f, 0.16f), 0.3f, 0f, planks);
        ceilingMat = Mat("Ceiling", new Color(0.5f, 0.5f, 0.48f), 0.05f, 0f, grime);
        chalkboard = Mat("Chalkboard", new Color(0.09f, 0.17f, 0.13f), 0.15f, 0f, grime);
        deskWood = Mat("DeskWood", new Color(0.55f, 0.4f, 0.25f), 0.3f, 0f, planks);
        darkWood = Mat("DarkWood", new Color(0.28f, 0.19f, 0.12f), 0.25f, 0f, planks);
        metal = Mat("Metal", new Color(0.35f, 0.36f, 0.37f), 0.45f, 0.6f, grime);
        lockerMetal = Mat("LockerMetal", new Color(0.33f, 0.4f, 0.45f), 0.35f, 0.4f, grime);
        lockerDoorMat = Mat("LockerDoor", new Color(0.37f, 0.44f, 0.5f), 0.4f, 0.4f, grime);
        paper = Mat("Paper", new Color(0.82f, 0.79f, 0.7f), 0.05f);
        photoMat = Mat("Photo", new Color(0.55f, 0.53f, 0.5f), 0.5f);
        black = Mat("PianoBlack", new Color(0.03f, 0.03f, 0.03f), 0.75f);
        keyWhite = Mat("PianoKey", new Color(0.72f, 0.7f, 0.64f), 0.25f);
        tube = Mat("FluorescentTube", new Color(0.9f, 0.95f, 1f), 0.5f, 0f, null, new Color(2f, 2.2f, 2.4f));
        exitGreen = Mat("ExitSign", new Color(0.1f, 0.6f, 0.25f), 0.3f, 0f, null, new Color(0.15f, 1.2f, 0.4f));
        lamp = Mat("IndicatorLamp", new Color(0.08f, 0.08f, 0.08f), 0.6f, 0f, null, Color.black);
        figure = Mat("Silhouette", new Color(0.004f, 0.004f, 0.004f), 0f);
        ground = Mat("GroundOutside", new Color(0.09f, 0.1f, 0.08f), 0.05f, 0f, grime);
        cork = Mat("Cork", new Color(0.5f, 0.36f, 0.22f), 0.05f, 0f, grime);
        plastic = Mat("Plastic", new Color(0.55f, 0.53f, 0.48f), 0.3f);
        plasticDark = Mat("PlasticDark", new Color(0.12f, 0.12f, 0.13f), 0.35f);
        glass = GlassMat("WindowGlass", new Color(0.55f, 0.65f, 0.75f, 0.12f));
        darkGlass = Mat("DarkGlass", new Color(0.05f, 0.06f, 0.07f), 0.9f);
        frame = Mat("WindowFrame", new Color(0.3f, 0.3f, 0.29f), 0.35f, 0.5f);
        redButton = Mat("RedButton", new Color(0.6f, 0.05f, 0.04f), 0.5f);
        brass = Mat("Brass", new Color(0.7f, 0.55f, 0.25f), 0.6f, 0.9f);
        diaryCover = Mat("DiaryCover", new Color(0.55f, 0.22f, 0.25f), 0.2f);
        bookCover = Mat("BookCover", new Color(0.12f, 0.16f, 0.3f), 0.2f);
        clockFace = Mat("ClockFace", new Color(0.8f, 0.78f, 0.72f), 0.3f);
        fuseMat = Mat("Fuse", new Color(0.75f, 0.8f, 0.8f), 0.8f, 0.3f);
        doorWood = Mat("DoorWood", new Color(0.42f, 0.3f, 0.2f), 0.25f, 0f, planks);
        doorMetal = Mat("DoorMetal", new Color(0.28f, 0.3f, 0.3f), 0.4f, 0.5f, grime);
        signPlate = Mat("SignPlate", new Color(0.8f, 0.8f, 0.76f), 0.3f);
        extinguisher = Mat("Extinguisher", new Color(0.55f, 0.05f, 0.04f), 0.5f);
    }

    static void SetupEnvironment()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.075f, 0.095f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.03f;
        RenderSettings.fogColor = new Color(0.02f, 0.024f, 0.032f);
        RenderSettings.reflectionIntensity = 0.3f;

        // 실시간 조명만 쓴다 (라이트맵을 굽지 않음)
        const string lightingPath = "Assets/Settings/SchoolLighting.lighting";
        var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingPath);
        if (lighting == null)
        {
            lighting = new LightingSettings { name = "SchoolLighting" };
            AssetDatabase.CreateAsset(lighting, lightingPath);
        }
        lighting.bakedGI = false;
        lighting.realtimeGI = false;
        Lightmapping.lightingSettings = lighting;

        // 북서쪽 창문으로 들어오는 달빛
        var moon = new GameObject("Moonlight").AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.55f, 0.65f, 0.95f);
        moon.intensity = 0.8f;
        moon.shadows = LightShadows.Soft;
        moon.transform.rotation = Quaternion.Euler(38f, 160f, 0f);
        RenderSettings.sun = moon;

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = HorrorVolumeProfile();
    }

    // ───────── 건물 뼈대 ─────────

    static void BuildStructure(Transform world)
    {
        var s = Group("Structure", world);

        Box("Floor", s, new Vector3(11f, -0.1f, 4.75f), new Vector3(34.6f, 0.2f, 13.1f), terrazzo, true, 1.2f);
        Box("Floor_Classroom", s, new Vector3(6.5f, 0.005f, 7f), new Vector3(9f, 0.01f, 8f), woodFloor, false, 2f);
        Box("Floor_Music", s, new Vector3(16.5f, 0.005f, 7f), new Vector3(9f, 0.01f, 8f), woodFloor, false, 2f);
        Box("Floor_Broadcast", s, new Vector3(25.5f, 0.005f, 5.5f), new Vector3(5f, 0.01f, 5f), woodFloor, false, 2f);
        Box("Ceiling", s, new Vector3(11f, H + 0.1f, 4.75f), new Vector3(34.6f, 0.2f, 13.1f), ceilingMat, true, 2f);
        Box("Ground_Outside", s, new Vector3(11f, -0.35f, 4.75f), new Vector3(120f, 0.5f, 100f), ground, true, 4f);

        // 로비
        Wall("Lobby_West", s, new Vector3(-6f, 0f, -1.5f), new Vector3(-6f, 0f, 4.5f), H, T, Wainscot, wallLower, wallUpper, Opening.Door(3f, 1.6f, 2.3f));
        Wall("Lobby_South", s, new Vector3(-6f, 0f, -1.5f), new Vector3(0f, 0f, -1.5f), H, T, Wainscot, wallLower, wallUpper, Opening.Window(3f));
        Wall("Lobby_North", s, new Vector3(-6f, 0f, 4.5f), new Vector3(0f, 0f, 4.5f), H, T, Wainscot, wallLower, wallUpper);
        Wall("Lobby_East_S", s, new Vector3(0f, 0f, -1.5f), new Vector3(0f, 0f, 0f), H, T, Wainscot, wallLower, wallUpper);
        Wall("Lobby_East_N", s, new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 4.5f), H, T, Wainscot, wallLower, wallUpper);
        DoorFrame(s, new Vector3(-6f, 0f, 1.5f), false, 1.6f, 2.3f);
        Windows(s, new Vector3(-6f, 0f, -1.5f), new Vector3(0f, 0f, -1.5f), 3f);

        // 복도
        float[] corridorWindows = { 2.5f, 6.5f, 10.5f, 14.5f, 18.5f, 22.5f, 26.5f };
        Wall("Corridor_South", s, Vector3.zero, new Vector3(28f, 0f, 0f), H, T, Wainscot, wallLower, wallUpper, System.Array.ConvertAll(corridorWindows, x => Opening.Window(x)));
        Windows(s, Vector3.zero, new Vector3(28f, 0f, 0f), corridorWindows);
        Wall("Corridor_North", s, new Vector3(0f, 0f, 3f), new Vector3(28f, 0f, 3f), H, T, Wainscot, wallLower, wallUpper,
            Opening.Door(10f), Opening.Door(13f), Opening.Door(24f));
        foreach (float x in new[] { 10f, 13f, 24f }) DoorFrame(s, new Vector3(x, 0f, 3f), true, 1f, 2.1f);
        Wall("East_End", s, new Vector3(28f, 0f, 0f), new Vector3(28f, 0f, 8f), H, T, Wainscot, wallLower, wallUpper);

        // 교실·음악실 (북쪽 벽에 창문 셋)
        float[] roomWindows = { 1.8f, 4.5f, 7.2f };
        foreach (var (name, x0) in new[] { ("Classroom", 2f), ("Music", 12f) })
        {
            Wall($"{name}_West", s, new Vector3(x0, 0f, 3f), new Vector3(x0, 0f, 11f), H, T, Wainscot, wallLower, wallUpper);
            Wall($"{name}_East", s, new Vector3(x0 + 9f, 0f, 3f), new Vector3(x0 + 9f, 0f, 11f), H, T, Wainscot, wallLower, wallUpper);
            Wall($"{name}_North", s, new Vector3(x0, 0f, 11f), new Vector3(x0 + 9f, 0f, 11f), H, T, Wainscot, wallLower, wallUpper,
                System.Array.ConvertAll(roomWindows, x => Opening.Window(x)));
            Windows(s, new Vector3(x0, 0f, 11f), new Vector3(x0 + 9f, 0f, 11f), roomWindows);
        }

        // 방송실 (창문 없음)
        Wall("Broadcast_West", s, new Vector3(23f, 0f, 3f), new Vector3(23f, 0f, 8f), H, T, Wainscot, wallLower, wallUpper);
        Wall("Broadcast_North", s, new Vector3(23f, 0f, 8f), new Vector3(28f, 0f, 8f), H, T, Wainscot, wallLower, wallUpper);
    }

    static void DoorFrame(Transform parent, Vector3 center, bool alongX, float width, float height)
    {
        var f = Group("DoorFrame", parent, center, alongX ? 0f : 90f);
        const float w = 0.07f;
        float depth = T + 0.06f;
        Box("Left", f, new Vector3(-width / 2f - w / 2f, height / 2f, 0f), new Vector3(w, height, depth), frame, false);
        Box("Right", f, new Vector3(width / 2f + w / 2f, height / 2f, 0f), new Vector3(w, height, depth), frame, false);
        Box("Top", f, new Vector3(0f, height + w / 2f, 0f), new Vector3(width + w * 2f, w, depth), frame, false);
    }

    static void Windows(Transform parent, Vector3 a, Vector3 b, params float[] centers)
    {
        Vector3 dir = (b - a).normalized;
        bool alongX = Mathf.Abs(dir.x) > 0.5f;
        const float w = 1.6f, y0 = 1f, y1 = 2.5f, bar = 0.06f, depth = 0.12f;
        foreach (float c in centers)
        {
            var root = Group("Window", parent, a + dir * c, alongX ? 0f : 90f);
            Box("Sill", root, new Vector3(0f, y0 + 0.02f, 0f), new Vector3(w + 0.1f, 0.04f, T + 0.08f), frame);
            Box("Top", root, new Vector3(0f, y1 - bar / 2f, 0f), new Vector3(w, bar, depth), frame);
            Box("Left", root, new Vector3(-w / 2f + bar / 2f, (y0 + y1) / 2f, 0f), new Vector3(bar, y1 - y0, depth), frame);
            Box("Right", root, new Vector3(w / 2f - bar / 2f, (y0 + y1) / 2f, 0f), new Vector3(bar, y1 - y0, depth), frame);
            Box("Mullion", root, new Vector3(0f, (y0 + y1) / 2f, 0f), new Vector3(bar * 0.8f, y1 - y0, depth), frame);
            Box("Transom", root, new Vector3(0f, 1.95f, 0f), new Vector3(w, bar * 0.8f, depth), frame);
            var pane = Box("Glass", root, new Vector3(0f, (y0 + y1) / 2f, 0f), new Vector3(w, y1 - y0, 0.01f), glass);
            pane.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Examine(root.gameObject, "창문은 녹이 슬어 꿈쩍도 하지 않는다.");
        }
    }

    // ───────── 플레이어·관리자 ─────────

    static GameObject BuildPlayer(Vector3 position, float yaw)
    {
        // Ignore Raycast 레이어: 상호작용·머리 위 검사 레이가 플레이어 자신의 콜라이더에 맞지 않게
        var player = new GameObject("Player") { tag = "Player", layer = LayerMask.NameToLayer("Ignore Raycast") };
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.75f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.875f, 0f);
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.04f;
        cc.minMoveDistance = 0f;
        player.AddComponent<Inventory>();

        var cameraRoot = Group("CameraRoot", player.transform, new Vector3(0f, 1.6f, 0f));
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.SetParent(cameraRoot, false);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 80f;
        cam.fieldOfView = 68f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.01f, 0.016f);
        camGo.AddComponent<AudioListener>();
        var camData = cam.GetUniversalAdditionalCameraData();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        var flashGo = new GameObject("Flashlight");
        flashGo.transform.SetParent(player.transform, false);
        flashGo.transform.localPosition = new Vector3(0.18f, 1.45f, 0.1f);
        var spot = flashGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 70f;
        spot.innerSpotAngle = 30f;
        spot.range = 24f;
        spot.intensity = 16f;
        spot.color = new Color(1f, 0.94f, 0.82f);
        spot.shadows = LightShadows.Soft;
        spot.shadowNearPlane = 0.1f;
        playerFlashlight = flashGo.AddComponent<Flashlight>();
        Set(playerFlashlight, "spot", spot);
        Set(playerFlashlight, "followTarget", camGo.transform);

        var controller = player.AddComponent<FirstPersonController>();
        Set(controller, "cameraRoot", cameraRoot);
        var interactor = player.AddComponent<PlayerInteractor>();
        Set(interactor, "viewCamera", cam);
        var steps = player.AddComponent<Footsteps>();
        Set(steps, "controller", controller);
        var stepSource = player.GetComponent<AudioSource>();
        stepSource.playOnAwake = false;
        stepSource.spatialBlend = 0f;
        return player;
    }

    static void BuildGameManager(GameUIRefs ui)
    {
        var go = new GameObject("GameManager");
        var gm = go.AddComponent<GameManager>();
        Set(gm, "pauseMenu", ui.pause);
        go.AddComponent<IntroSequence>();
        ending = go.AddComponent<EndingSequence>();
        go.AddComponent<AmbienceController>();
        go.GetComponent<AudioSource>().playOnAwake = false;
    }

    // ───────── 로비 ─────────

    static void BuildLobby(Transform world)
    {
        var room = Group("Lobby", world);

        frontDoor = HingedDoor("FrontDoor", room, new Vector3(-6f, 0f, 0.7f), -90f, 1.6f, 2.3f, doorMetal, 95f);
        Set(frontDoor, "startOpen", true);
        Set(frontDoor, "lockedMessage", "꿈쩍도 하지 않는다. 밖에서 잠긴 것 같다.");
        Set(frontDoor, "openPrompt", "현관문 열기");
        Set(frontDoor, "closePrompt", "현관문 닫기");

        // 비상구 표시등
        Box("ExitSign", room, new Vector3(-5.87f, 2.62f, 1.5f), new Vector3(0.04f, 0.16f, 0.42f), exitGreen, false);
        WorldText("ExitSignText", room, "비상구", new Vector3(-5.845f, 2.62f, 1.5f), Vector3.right, 1.1f, new Vector2(0.4f, 0.15f), gothic, gothic.material, new Color(0.9f, 1f, 0.9f));
        PointLight("ExitGlow", room, new Vector3(-5.6f, 2.5f, 1.5f), new Color(0.2f, 1f, 0.35f), 0.5f, 3.5f);

        Box("Notice", room, new Vector3(-5.2f, 1.6f, -1.395f), new Vector3(0.42f, 0.3f, 0.005f), paper, false);
        WorldText("NoticeText", room, "출입 금지\n<size=45%>본 건물은 2026년 10월 24일 철거 예정입니다.</size>", new Vector3(-5.2f, 1.6f, -1.39f), Vector3.forward,
            0.9f, new Vector2(0.38f, 0.26f), gothic, gothicLit, new Color(0.55f, 0.06f, 0.05f));

        // 경비실 창구: 스탠드 불빛 아래 손전등과 순찰 일지
        Box("GuardCounter", room, new Vector3(-3f, 0.5f, -1.12f), new Vector3(1.8f, 1f, 0.55f), darkWood);
        WorldText("GuardSign", room, "경 비 실", new Vector3(-3f, 2.75f, -1.39f), Vector3.forward, 1.4f, new Vector2(1f, 0.2f), gothic, gothicLit, new Color(0.75f, 0.73f, 0.68f));
        Box("LampBase", room, new Vector3(-2.35f, 1.02f, -1.2f), new Vector3(0.12f, 0.03f, 0.12f), metal, false);
        Box("LampStem", room, new Vector3(-2.35f, 1.2f, -1.2f), new Vector3(0.02f, 0.35f, 0.02f), metal, false);
        Box("LampShade", room, new Vector3(-2.35f, 1.38f, -1.15f), new Vector3(0.16f, 0.1f, 0.16f), darkWood, false);
        PointLight("DeskLamp", room, new Vector3(-2.45f, 1.3f, -1.05f), new Color(1f, 0.72f, 0.42f), 0.8f, 2.8f);

        var torch = Box("Flashlight", room, new Vector3(-3.25f, 1.03f, -1.05f), new Vector3(0.2f, 0.05f, 0.05f), metal);
        Box("Lens", torch.transform, new Vector3(0.105f, 0f, 0f), new Vector3(0.012f, 0.062f, 0.062f), fuseMat, false);
        flashlightPickup = Pickup(torch, content.flashlight, new Vector3(0.32f, 0.14f, 0.16f));

        var patrol = Box("PatrolLog", room, new Vector3(-2.8f, 1.012f, -1.08f), new Vector3(0.22f, 0.02f, 0.3f), paper);
        Readable(patrol, content.patrolLog, "순찰 일지 읽기", false, "10월 17일… 오늘 날짜에서 멈춰 있다.");

        var shoes = Box("ShoeLockers", room, new Vector3(-3f, 0.75f, 4.22f), new Vector3(3.6f, 1.5f, 0.36f), deskWood);
        for (int i = 1; i < 6; i++) Box("Divider", shoes.transform, new Vector3(-1.8f + i * 0.6f, 0f, -0.185f), new Vector3(0.02f, 1.5f, 0.01f), darkWood, false);
        for (int j = 1; j < 4; j++) Box("Shelf", shoes.transform, new Vector3(0f, -0.75f + j * 0.375f, -0.185f), new Vector3(3.6f, 0.02f, 0.01f), darkWood, false);
        Examine(shoes, "신발장. 먼지 쌓인 실내화 몇 켤레가 그대로 남아 있다.");

        lobbyLight = CeilingLight("Light_Lobby", room, new Vector3(-3f, H, 1.5f), FlickerLight.Mode.Flicker, 2f, 7f);
        lobbySpeaker = SpeakerBox("Speaker_Lobby", room, new Vector3(-3f, 2.9f, 4.33f), new Vector3(0.36f, 0.26f, 0.14f));

        // 복도 첫 형광등(x=3) 바로 앞에 세워서 역광으로 윤곽이 보이게 한다
        silhouette = BuildSilhouette(room, new Vector3(4.2f, 0f, 1.5f));

        introTrigger = Trigger("Trigger_IntroSlam", room, new Vector3(-4f, 1.2f, 1.5f), new Vector3(0.6f, 2.4f, 6f));
        endingTrigger = Trigger("Trigger_Ending", room, new Vector3(-6.9f, 1.2f, 1.5f), new Vector3(0.6f, 2.4f, 2.4f));
    }

    static GameObject BuildSilhouette(Transform parent, Vector3 position)
    {
        var root = Group("Silhouette", parent, position, -90f);
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        body.transform.localScale = new Vector3(0.42f, 0.62f, 0.28f);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root, false);
        head.transform.localPosition = new Vector3(0f, 1.47f, 0f);
        head.transform.localScale = new Vector3(0.23f, 0.25f, 0.23f);
        foreach (var part in new[] { body, head })
        {
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<MeshRenderer>().sharedMaterial = figure;
        }
        // 뒤쪽 바닥과 벽을 비추는 희미한 빛: 검은 윤곽이 드러나고, 사라질 때 함께 꺼진다
        PointLight("BackLight", root, new Vector3(0f, 1.3f, 1.3f), new Color(0.7f, 0.8f, 1f), 2f, 5f);
        var apparition = root.gameObject.AddComponent<Apparition>();
        Set(apparition, "lightsToGlitch", new[] { lobbyLight });
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    // ───────── 복도 ─────────

    static void BuildCorridor(Transform world)
    {
        var room = Group("Corridor", world);

        corridorLights = new[]
        {
            CeilingLight("Light_Corridor_1", room, new Vector3(3f, H, 1.5f), FlickerLight.Mode.On, 2f, 7f),
            CeilingLight("Light_Corridor_2", room, new Vector3(9f, H, 1.5f), FlickerLight.Mode.Flicker, 2f, 7f),
            CeilingLight("Light_Corridor_3", room, new Vector3(15f, H, 1.5f), FlickerLight.Mode.Off, 2f, 7f),
            CeilingLight("Light_Corridor_4", room, new Vector3(20.5f, H, 1.5f), FlickerLight.Mode.Flicker, 1.6f, 6f),
            CeilingLight("Light_Corridor_5", room, new Vector3(26f, H, 1.5f), FlickerLight.Mode.Off, 2f, 7f)
        };
        Set(silhouette.GetComponent<Apparition>(), "lightsToGlitch", new[] { lobbyLight, corridorLights[0] });

        corridorSpeakers = new[]
        {
            SpeakerBox("Speaker_Corridor_1", room, new Vector3(7f, 2.9f, 2.83f), new Vector3(0.36f, 0.26f, 0.14f)),
            SpeakerBox("Speaker_Corridor_2", room, new Vector3(19f, 2.9f, 2.83f), new Vector3(0.36f, 0.26f, 0.14f))
        };

        RoomSign(room, 10.75f, "2학년 3반");
        RoomSign(room, 13.75f, "음악실");
        RoomSign(room, 24.75f, "방송실");

        Box("Poster", room, new Vector3(5f, 1.7f, 2.895f), new Vector3(0.5f, 0.35f, 0.005f), paper, false);
        WorldText("PosterText", room, "복도에서\n뛰지 맙시다", new Vector3(5f, 1.7f, 2.89f), Vector3.back, 1.2f, new Vector2(0.46f, 0.3f), gothic, gothicLit, new Color(0.2f, 0.25f, 0.5f));

        Box("FireExtinguisher", room, new Vector3(16.5f, 0.25f, 0.22f), new Vector3(0.15f, 0.5f, 0.15f), extinguisher);

        // 복도 끝에 쌓아 둔 의자 (의자 끄는 소리가 나는 곳)
        Chair(room, new Vector3(27.3f, 0f, 0.9f), 90f);
        Chair(room, new Vector3(27.3f, 0f, 1.6f), 80f);
        var tipped = Chair(room, new Vector3(26.7f, 0.2f, 2.3f), 30f);
        tipped.localRotation = Quaternion.Euler(0f, 30f, 90f);

        flickerTrigger = Trigger("Trigger_CorridorLightOff", room, new Vector3(8.3f, 1.2f, 1.5f), new Vector3(0.5f, 2.4f, 3f));
        chairDragTrigger = Trigger("Trigger_ChairDrag", room, new Vector3(10f, 1.2f, 1.6f), new Vector3(1.2f, 2.4f, 2f), armed: false);
        chairDragSound = Emitter("Sound_ChairDrag", room, new Vector3(27.2f, 0.5f, 1.5f), SoundKind.ChairDrag, 1f, 40f);
    }

    static void RoomSign(Transform parent, float x, string label)
    {
        var sign = Group($"Sign_{label}", parent, new Vector3(x, 2.55f, 2.69f));
        Box("Plate", sign, Vector3.zero, new Vector3(0.04f, 0.2f, 0.42f), signPlate, false);
        Box("Bracket", sign, new Vector3(0f, 0.11f, 0.15f), new Vector3(0.02f, 0.02f, 0.12f), metal, false);
        WorldText("TextEast", sign, label, new Vector3(0.023f, 0f, 0f), Vector3.right, 1.1f, new Vector2(0.4f, 0.18f), gothic, gothicLit, Ink);
        WorldText("TextWest", sign, label, new Vector3(-0.023f, 0f, 0f), Vector3.left, 1.1f, new Vector2(0.4f, 0.18f), gothic, gothicLit, Ink);
    }

    // ───────── 2학년 3반 교실 ─────────

    static void BuildClassroom(Transform world)
    {
        var room = Group("Classroom_2-3", world);

        var door = SlidingDoor("ClassroomDoor", room, new Vector3(10f, 0f, 2.865f), new Vector3(-1f, 0f, 0f));
        Set(door, "openPrompt", "교실 문 열기");
        Set(door, "closePrompt", "교실 문 닫기");

        // 칠판 (서쪽 벽). 학생들은 서쪽을 보고 앉는다.
        Chalkboard(room, 2.13f, Vector3.right);
        WorldText("Board_Date", room, "10월 17일 (화)\n주번  한서진 · 이도현", new Vector3(2.157f, 1.85f, 7.95f), Vector3.right, 2.4f, new Vector2(1.9f, 0.6f), pen, penLit, Chalk, TextAlignmentOptions.TopRight);
        WorldText("Board_Exam", room, "중간고사 범위\n수학 3~5단원 / 국어 4단원까지", new Vector3(2.157f, 1.85f, 6.05f), Vector3.right, 2f, new Vector2(1.9f, 0.6f), pen, penLit, Chalk, TextAlignmentOptions.TopLeft);
        boardWriting = WorldText("Board_WhyNot", room, "왜 안 왔어?", new Vector3(2.157f, 1.3f, 7f), Vector3.right, 7f, new Vector2(3.6f, 1f), pen, penLit, new Color(0.9f, 0.88f, 0.84f)).gameObject;
        boardWriting.SetActive(false);
        stingerSound = Emitter("Sound_Stinger", room, new Vector3(3f, 1.5f, 7f), SoundKind.Stinger, 0.9f, 20f);

        // 교탁과 출석부
        Box("TeacherDesk", room, new Vector3(2.85f, 0.5f, 7f), new Vector3(0.5f, 1f, 1.1f), darkWood);
        var attendance = Box("AttendanceBook", room, new Vector3(2.85f, 1.015f, 6.85f), new Vector3(0.24f, 0.03f, 0.32f), bookCover);
        Readable(attendance, content.attendance, "출석부 읽기", false, "17번… 한서진. 10월 18일부터 결석으로 되어 있다.");

        // 자리 배치표 (칠판 오른쪽)
        var chart = Box("SeatChart", room, new Vector3(2.11f, 1.5f, 9.65f), new Vector3(0.01f, 0.42f, 0.32f), paper);
        WorldText("SeatChartTitle", room, "자리 배치표", new Vector3(2.118f, 1.67f, 9.65f), Vector3.right, 0.45f, new Vector2(0.3f, 0.06f), gothic, gothicLit, Ink);
        WorldText("SeatChartGrid", room, $"<mspace=0.65em>{GameContentBuilder.SeatGrid()}</mspace>", new Vector3(2.118f, 1.47f, 9.65f), Vector3.right, 0.32f, new Vector2(0.28f, 0.26f), gothic, gothicLit, Ink);
        Readable(chart, content.seatChart, "자리 배치표 보기", false, "서진이 자리가 어디였더라…");

        // 시계: 6시 42분에 멈춰 있다
        var clock = Box("Clock", room, new Vector3(2.13f, 2.62f, 7f), new Vector3(0.05f, 0.34f, 0.34f), clockFace);
        ClockHand(room, 201f, 0.09f, 0.018f);
        ClockHand(room, 252f, 0.14f, 0.012f);
        Examine(clock, "시계가 6시 42분에 멈춰 있다.");

        // 책상 20개
        var rng = new System.Random(4);
        for (int r = 0; r < 5; r++)
        for (int c = 0; c < 4; c++)
        {
            int number = GameContentBuilder.Seats[r, c];
            var drawer = Desk(room, new Vector3(3.9f + r * 1.25f, 0f, 4.6f + c * 1.6f), number);
            float jitter = (float)(rng.NextDouble() - 0.5);
            if (number == 20)
            {
                var fallen = Chair(room, new Vector3(3.9f + r * 1.25f + 0.65f, 0.2f, 4.6f + c * 1.6f + 0.2f), 40f);
                fallen.localRotation = Quaternion.Euler(0f, 40f, 90f);
            }
            else Chair(room, new Vector3(3.9f + r * 1.25f + 0.45f + jitter * 0.15f, 0f, 4.6f + c * 1.6f + jitter * 0.1f), jitter * 20f);

            switch (number)
            {
                case 17:
                    var diary = Box("Diary", drawer, new Vector3(0.06f, 0.047f, 0f), new Vector3(0.15f, 0.024f, 0.21f), diaryCover);
                    diaryNote = Readable(diary, content.diary, "일기장 읽기", true, "서진이… 전학 가려고 했던 거였구나.");
                    break;
                case 9: Note(drawer, content.noteRumor, "쪽지 읽기"); break;
                case 3: Note(drawer, content.noteTest, "시험지 보기"); break;
                case 12: Note(drawer, content.noteAudition, "공책 보기"); break;
            }
        }

        BuildLockers(room);

        // 뒤 게시판과 단체 사진
        Box("BulletinBoard", room, new Vector3(10.87f, 1.75f, 7f), new Vector3(0.03f, 0.9f, 2.4f), cork);
        var photo = Box("ClassPhoto", room, new Vector3(10.85f, 1.8f, 7.55f), new Vector3(0.01f, 0.2f, 0.28f), photoMat);
        Readable(photo, content.photo, "사진 뒤집어 보기", false, "맨 끝에 서진이가 있다. 그 옆에… 나도 있다.");
        Box("Notice_Lunch", room, new Vector3(10.85f, 1.72f, 6.55f), new Vector3(0.005f, 0.32f, 0.24f), paper, false);
        WorldText("Notice_LunchText", room, "급식 당번표\n<size=60%>1주  1~5번\n2주  6~10번\n3주  11~15번\n4주  16~20번</size>",
            new Vector3(10.845f, 1.72f, 6.55f), Vector3.left, 0.42f, new Vector2(0.22f, 0.3f), gothic, gothicLit, Ink);

        classroomSpeaker = SpeakerBox("Speaker_Classroom", room, new Vector3(2.17f, 2.85f, 9.9f), new Vector3(0.14f, 0.26f, 0.36f));
        CeilingLight("Light_Classroom_1", room, new Vector3(5f, H, 5.5f), FlickerLight.Mode.Off, withLight: false);
        CeilingLight("Light_Classroom_2", room, new Vector3(8f, H, 8.5f), FlickerLight.Mode.Off, withLight: false);

        classroomEntry = Trigger("Trigger_ClassroomEntry", room, new Vector3(10f, 1.2f, 3.8f), new Vector3(1.4f, 2.4f, 1f));
        classroomReentry = Trigger("Trigger_ClassroomReentry", room, new Vector3(10f, 1.2f, 3.8f), new Vector3(1.4f, 2.4f, 1f), armed: false);
    }

    static void Chalkboard(Transform room, float x, Vector3 facing)
    {
        float s = Mathf.Sign(facing.x);
        Box("Chalkboard", room, new Vector3(x, 1.55f, 7f), new Vector3(0.04f, 1.2f, 4f), chalkboard);
        Box("BoardFrame_Top", room, new Vector3(x + s * 0.01f, 2.175f, 7f), new Vector3(0.06f, 0.05f, 4.1f), darkWood, false);
        Box("BoardFrame_Bottom", room, new Vector3(x + s * 0.01f, 0.925f, 7f), new Vector3(0.06f, 0.05f, 4.1f), darkWood, false);
        Box("ChalkTray", room, new Vector3(x + s * 0.06f, 0.94f, 7f), new Vector3(0.1f, 0.025f, 4f), darkWood, false);
    }

    static void ClockHand(Transform room, float angle, float length, float width)
    {
        var pivot = Group("ClockHand", room, new Vector3(2.162f, 2.62f, 7f));
        pivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
        Box("Hand", pivot, new Vector3(0f, length / 2f, 0f), new Vector3(0.005f, length, width), black, false);
    }

    // 책상: 어디를 봐도 서랍이 열린다. 서랍 Transform을 돌려준다 (안에 물건을 넣을 수 있게).
    static Transform Desk(Transform parent, Vector3 position, int number)
    {
        var desk = Group($"Desk_{number:00}", parent, position);
        Box("Top", desk, new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.03f, 0.66f), deskWood);
        foreach (float sx in new[] { -1f, 1f })
        foreach (float sz in new[] { -1f, 1f })
            Box("Leg", desk, new Vector3(sx * 0.2f, 0.355f, sz * 0.3f), new Vector3(0.03f, 0.71f, 0.03f), metal, false);
        Box("Shelf", desk, new Vector3(0f, 0.585f, 0f), new Vector3(0.42f, 0.012f, 0.62f), metal, false);
        Box("FrontPanel", desk, new Vector3(-0.215f, 0.645f, 0f), new Vector3(0.012f, 0.11f, 0.62f), metal);
        var drawerBox = Box("Drawer", desk, new Vector3(0.02f, 0.63f, 0f), new Vector3(0.38f, 0.07f, 0.56f), darkWood);
        Box("Handle", drawerBox.transform, new Vector3(0.195f, 0f, 0f), new Vector3(0.01f, 0.02f, 0.12f), metal, false);
        var drawer = desk.gameObject.AddComponent<Drawer>();
        Set(drawer, "movingPart", drawerBox.transform);
        Set(drawer, "openOffset", new Vector3(0.3f, 0f, 0f));
        return drawerBox.transform;
    }

    static void Note(Transform drawer, DocumentData document, string prompt)
    {
        var note = Box("Note", drawer, new Vector3(0.08f, 0.037f, 0.02f), new Vector3(0.14f, 0.004f, 0.18f), paper);
        note.GetComponent<BoxCollider>().size = new Vector3(0.14f, 0.02f, 0.18f);
        Readable(note, document, prompt);
    }

    static Transform Chair(Transform parent, Vector3 position, float yaw)
    {
        var chair = Group("Chair", parent, position, yaw);
        Box("Seat", chair, new Vector3(0f, 0.44f, 0f), new Vector3(0.38f, 0.025f, 0.38f), deskWood);
        Box("Back", chair, new Vector3(0.18f, 0.68f, 0f), new Vector3(0.025f, 0.3f, 0.36f), deskWood, false);
        foreach (float sx in new[] { -1f, 1f })
        foreach (float sz in new[] { -1f, 1f })
            Box("Leg", chair, new Vector3(sx * 0.17f, 0.215f, sz * 0.17f), new Vector3(0.025f, 0.43f, 0.025f), metal, false);
        return chair;
    }

    static void BuildLockers(Transform room)
    {
        var lockers = Group("Lockers", room);
        Examine(lockers.gameObject, "다른 사물함들은 녹이 슬어 열리지 않는다.");
        Box("Base", lockers, new Vector3(10.66f, 0.05f, 7.11f), new Vector3(0.46f, 0.1f, 4.3f), metal);

        for (int row = 0; row < 2; row++)
        for (int k = 0; k < 10; k++)
        {
            int number = row == 0 ? k + 1 : k + 11;
            float z = 9f - k * 0.42f;
            float y = row == 0 ? 0.775f : 0.325f;
            if (number == 17)
            {
                BuildLocker17(lockers, new Vector3(10.66f, y, z));
                continue;
            }
            var cell = Box($"Locker_{number:00}", lockers, new Vector3(10.66f, y, z), new Vector3(0.44f, 0.44f, 0.41f), lockerMetal);
            Box("Door", cell.transform, new Vector3(-0.225f, 0f, 0f), new Vector3(0.01f, 0.4f, 0.37f), lockerDoorMat, false);
            WorldText("Number", cell.transform, number.ToString(), new Vector3(-0.232f, 0.13f, 0f), Vector3.left, 0.6f, new Vector2(0.2f, 0.07f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f));
        }
    }

    static void BuildLocker17(Transform parent, Vector3 position)
    {
        var cell = Group("Locker_17", parent, position);
        Box("Back", cell, new Vector3(0.21f, 0f, 0f), new Vector3(0.02f, 0.44f, 0.41f), lockerMetal);
        Box("Top", cell, new Vector3(0f, 0.21f, 0f), new Vector3(0.44f, 0.02f, 0.41f), lockerMetal);
        Box("Bottom", cell, new Vector3(0f, -0.21f, 0f), new Vector3(0.44f, 0.02f, 0.41f), lockerMetal);
        Box("SideN", cell, new Vector3(0f, 0f, 0.195f), new Vector3(0.44f, 0.44f, 0.02f), lockerMetal);
        Box("SideS", cell, new Vector3(0f, 0f, -0.195f), new Vector3(0.44f, 0.44f, 0.02f), lockerMetal);

        // 경첩은 북쪽 모서리, 열면 교실 쪽(-x)으로 열린다
        var pivot = Group("DoorPivot", cell, new Vector3(-0.225f, 0f, 0.19f), 90f);
        var leaf = Box("Leaf", pivot, new Vector3(0.19f, 0f, 0f), new Vector3(0.37f, 0.4f, 0.012f), lockerDoorMat);
        WorldText("Number", leaf.transform, "17", new Vector3(0f, 0.13f, -0.008f), Vector3.back, 0.6f, new Vector2(0.2f, 0.07f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f));
        lockerDoor = pivot.gameObject.AddComponent<Door>();
        Set(lockerDoor, "locked", true);
        Set(lockerDoor, "openAngle", 100f);
        Set(lockerDoor, "openDuration", 0.7f);
        Set(lockerDoor, "openPrompt", "사물함 열기");
        Set(lockerDoor, "closePrompt", "사물함 닫기");

        padlock = Box("Padlock", leaf.transform, new Vector3(0.32f, -0.03f, -0.025f), new Vector3(0.045f, 0.06f, 0.025f), brass, false);
        Box("Shackle", padlock.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.03f, 0.03f, 0.008f), metal, false);
        lockerLock = leaf.AddComponent<CombinationLock>();
        Set(lockerLock, "code", "0302");

        var key = Box("MusicRoomKey", cell, new Vector3(-0.05f, -0.192f, 0.09f), new Vector3(0.07f, 0.012f, 0.03f), brass);
        Pickup(key, content.musicKey, new Vector3(0.14f, 0.06f, 0.12f));
        var sheet = Box("SheetMusic", cell, new Vector3(-0.04f, -0.198f, -0.07f), new Vector3(0.17f, 0.003f, 0.2f), paper);
        sheet.GetComponent<BoxCollider>().size = new Vector3(0.17f, 0.02f, 0.2f);
        Readable(sheet, content.sheetMusic, "악보 보기", true, "숫자로 적은 악보… 서진이는 늘 이렇게 적었지.");
    }

    // ───────── 음악실 ─────────

    static void BuildMusicRoom(Transform world)
    {
        var room = Group("MusicRoom", world);

        var door = SlidingDoor("MusicRoomDoor", room, new Vector3(13f, 0f, 2.865f), new Vector3(1f, 0f, 0f));
        Set(door, "locked", true);
        Set(door, "requiredKey", content.musicKey);
        Set(door, "lockedMessage", "잠겨 있다. 열쇠가 있어야 할 것 같다.");
        Set(door, "openPrompt", "음악실 문 열기");
        Set(door, "closePrompt", "음악실 문 닫기");

        // 업라이트 피아노 (북쪽 벽)
        var pianoRoot = Group("Piano", room, new Vector3(18f, 0f, 10.55f));
        Box("Body", pianoRoot, new Vector3(0f, 0.65f, 0.05f), new Vector3(1.5f, 1.3f, 0.5f), black);
        Box("KeyBed", pianoRoot, new Vector3(0f, 0.72f, -0.33f), new Vector3(1.4f, 0.05f, 0.3f), black);
        Box("Cheek_L", pianoRoot, new Vector3(-0.67f, 0.8f, -0.33f), new Vector3(0.06f, 0.12f, 0.3f), black);
        Box("Cheek_R", pianoRoot, new Vector3(0.67f, 0.8f, -0.33f), new Vector3(0.06f, 0.12f, 0.3f), black);
        Box("MusicStand", pianoRoot, new Vector3(0f, 1.05f, -0.22f), new Vector3(0.7f, 0.22f, 0.02f), black, false);
        piano = pianoRoot.gameObject.AddComponent<PianoPuzzle>();

        for (int i = 0; i < 7; i++)
        {
            var key = Box($"Key_{PianoPuzzle.NoteNames[i]}", pianoRoot, new Vector3(-0.45f + i * 0.15f, 0.758f, -0.36f), new Vector3(0.142f, 0.025f, 0.24f), keyWhite);
            var pianoKey = key.AddComponent<PianoKey>();
            Set(pianoKey, "puzzle", piano);
            Set(pianoKey, "note", i);
            // 건반에 붙인 계이름 스티커
            WorldText("Label", key.transform, PianoPuzzle.NoteNames[i], new Vector3(0f, 0.0135f, -0.08f), Vector3.up, 0.55f, new Vector2(0.12f, 0.06f), pen, penLit, Ink, TextAlignmentOptions.Center, Vector3.forward);
        }
        foreach (int i in new[] { 0, 1, 3, 4, 5 })
            Box("BlackKey", pianoRoot, new Vector3(-0.45f + (i + 0.5f) * 0.15f, 0.78f, -0.3f), new Vector3(0.08f, 0.03f, 0.14f), black, false);

        // 건반 아래 숨은 서랍: 피아노를 맞게 치면 열린다
        var drawerBox = Box("HiddenDrawer", pianoRoot, new Vector3(0f, 0.63f, -0.32f), new Vector3(0.6f, 0.08f, 0.32f), darkWood);
        pianoDrawer = drawerBox.AddComponent<Drawer>();
        Set(pianoDrawer, "locked", true);
        Set(pianoDrawer, "openOffset", new Vector3(0f, 0f, -0.28f));
        Set(pianoDrawer, "lockedMessage", "건반 아래에 작은 서랍이 있다. 꽉 잠겨 있다.");
        var fuse = Box("Fuse", drawerBox.transform, new Vector3(-0.12f, 0.053f, -0.08f), new Vector3(0.08f, 0.025f, 0.025f), fuseMat);
        Pickup(fuse, content.fuse, new Vector3(0.14f, 0.07f, 0.1f));
        var bkey = Box("BroadcastRoomKey", drawerBox.transform, new Vector3(0.12f, 0.046f, -0.08f), new Vector3(0.07f, 0.012f, 0.03f), brass);
        Pickup(bkey, content.broadcastKey, new Vector3(0.14f, 0.06f, 0.12f));

        // 칠판 (서쪽 벽) 구석에 남은 노래 뒷부분
        Chalkboard(room, 12.13f, Vector3.right);
        for (int i = 0; i < 5; i++)
            Box("StaffLine", room, new Vector3(12.152f, 1.95f + i * 0.04f, 6.2f), new Vector3(0.002f, 0.006f, 2.2f), signPlate, false);
        var boardText = WorldText("Board_Song", room, "<size=70%>지우지 마세요!!</size>\n서진이 노래 — 뒷부분\n<size=150%>5    3    2   ‖</size>",
            new Vector3(12.157f, 1.5f, 7.9f), Vector3.right, 2.4f, new Vector2(1.9f, 1.1f), pen, penLit, Chalk);
        Readable(boardText.gameObject, content.musicBoard, "칠판 글씨 자세히 보기");
        boardText.gameObject.AddComponent<BoxCollider>().size = new Vector3(1.9f, 1.1f, 0.02f);

        // 숫자보 포스터 (동쪽 벽)
        var poster = Box("NumberPoster", room, new Vector3(20.885f, 1.6f, 6.2f), new Vector3(0.01f, 0.85f, 0.6f), paper);
        WorldText("PosterTitle", room, "숫자 악보 읽는 법", new Vector3(20.878f, 1.93f, 6.2f), Vector3.left, 0.75f, new Vector2(0.56f, 0.1f), gothic, gothicLit, Ink);
        WorldText("PosterBody", room, "<mspace=0.7em>1 도\n2 레\n3 미\n4 파\n5 솔\n6 라\n7 시</mspace>", new Vector3(20.878f, 1.5f, 6.2f), Vector3.left, 0.7f, new Vector2(0.3f, 0.6f), gothic, gothicLit, Ink);
        Readable(poster, content.numberPoster, "포스터 읽기");

        // 음악가 초상화 액자들 (얼굴은 어둡다)
        for (int i = 0; i < 3; i++)
        {
            var portrait = Box("Portrait", room, new Vector3(20.88f, 2.25f, 8.2f + i * 0.9f - 0.9f), new Vector3(0.03f, 0.5f, 0.4f), darkWood, false);
            Box("Canvas", portrait.transform, new Vector3(-0.017f, 0f, 0f), new Vector3(0.005f, 0.42f, 0.32f), plasticDark, false);
        }

        // 반원으로 놓인 의자와 보면대
        for (int i = 0; i < 8; i++)
        {
            float angle = Mathf.Lerp(-70f, 70f, i / 7f) * Mathf.Deg2Rad;
            var p = new Vector3(16f + Mathf.Sin(angle) * 2.4f, 0f, 7.5f - Mathf.Cos(angle) * 2.4f);
            Chair(room, p, 90f - angle * Mathf.Rad2Deg);
        }
        foreach (float x in new[] { 15f, 16.5f, 18f })
        {
            var stand = Group("MusicStand", room, new Vector3(x, 0f, 6.3f));
            Box("Pole", stand, new Vector3(0f, 0.55f, 0f), new Vector3(0.02f, 1.1f, 0.02f), metal, false);
            var desk = Box("Desk", stand, new Vector3(0f, 1.12f, 0f), new Vector3(0.45f, 0.3f, 0.01f), metal, false);
            desk.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        }

        musicSpeaker = SpeakerBox("Speaker_Music", room, new Vector3(12.17f, 2.85f, 9.9f), new Vector3(0.14f, 0.26f, 0.36f));
        CeilingLight("Light_Music_1", room, new Vector3(15f, H, 5.5f), FlickerLight.Mode.Off, withLight: false);
        CeilingLight("Light_Music_2", room, new Vector3(18f, H, 8.5f), FlickerLight.Mode.Off, withLight: false);

        musicEntry = Trigger("Trigger_MusicEntry", room, new Vector3(13f, 1.2f, 3.8f), new Vector3(1.4f, 2.4f, 1f));
    }

    // ───────── 방송실 ─────────

    static void BuildBroadcastRoom(Transform world)
    {
        var room = Group("BroadcastRoom", world);

        var door = HingedDoor("BroadcastRoomDoor", room, new Vector3(23.5f, 0f, 3f), 0f, 1f, 2.1f, doorMetal, -95f);
        Set(door, "locked", true);
        Set(door, "requiredKey", content.broadcastKey);
        Set(door, "lockedMessage", "두꺼운 철문이 잠겨 있다.");
        Set(door, "openPrompt", "방송실 문 열기");
        Set(door, "closePrompt", "방송실 문 닫기");

        // 콘솔 (북쪽 벽)
        var consoleRoot = Group("Console", room, new Vector3(25.7f, 0f, 7.45f));
        Box("Desk", consoleRoot, new Vector3(0f, 0.4f, 0f), new Vector3(3.4f, 0.8f, 0.7f), plasticDark);
        Box("Panel", consoleRoot, new Vector3(-0.45f, 0.98f, 0.31f), new Vector3(2.4f, 0.36f, 0.04f), plasticDark);
        console = consoleRoot.gameObject.AddComponent<BroadcastConsole>();

        string[] zones = { "현관", "복도", "2학년 3반", "음악실", "교무실", "운동장" };
        var switches = new ZoneSwitch[zones.Length];
        for (int i = 0; i < zones.Length; i++)
        {
            float x = -1.05f + i * 0.3f;
            var sw = Group($"ZoneSwitch_{zones[i]}", consoleRoot, new Vector3(x, 0.8f, 0.05f));
            Box("Plate", sw, new Vector3(0f, 0.01f, 0f), new Vector3(0.12f, 0.02f, 0.16f), metal, false);
            var leverPivot = Group("LeverPivot", sw, new Vector3(0f, 0.02f, 0f));
            Box("Lever", leverPivot, new Vector3(0f, 0.04f, 0f), new Vector3(0.025f, 0.08f, 0.025f), metal, false);
            var lampBox = Box("Lamp", sw, new Vector3(0f, 0.027f, -0.06f), new Vector3(0.035f, 0.015f, 0.035f), lamp, false);
            var col = sw.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.05f, 0f);
            col.size = new Vector3(0.18f, 0.12f, 0.2f);
            switches[i] = sw.gameObject.AddComponent<ZoneSwitch>();
            Set(switches[i], "console", console);
            Set(switches[i], "zoneName", zones[i]);
            Set(switches[i], "shouldBeOn", i == 2 || i == 3);
            Set(switches[i], "lever", leverPivot);
            Set(switches[i], "lamp", lampBox.GetComponent<MeshRenderer>());
            WorldText($"Label_{zones[i]}", consoleRoot, zones[i], new Vector3(x, 1.02f, 0.287f), Vector3.back, 0.42f, new Vector2(0.28f, 0.08f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f));
        }
        Set(switches[0], "speakers", new[] { lobbySpeaker });
        Set(switches[1], "speakers", corridorSpeakers);
        Set(switches[2], "speakers", new[] { classroomSpeaker });
        Set(switches[3], "speakers", new[] { musicSpeaker });
        WorldText("Label_Zones", consoleRoot, "송출 구역", new Vector3(-0.3f, 1.12f, 0.287f), Vector3.back, 0.5f, new Vector2(0.6f, 0.08f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f));

        var lamps = new Renderer[3];
        for (int j = 0; j < 3; j++)
            lamps[j] = Box("PowerLamp", consoleRoot, new Vector3(-1.55f + j * 0.07f, 1.08f, 0.288f), new Vector3(0.03f, 0.03f, 0.01f), lamp, false).GetComponent<MeshRenderer>();
        WorldText("Label_Power", consoleRoot, "전원", new Vector3(-1.48f, 1.02f, 0.287f), Vector3.back, 0.4f, new Vector2(0.2f, 0.06f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f));

        // 테이프 데크
        var deck = Group("TapeDeck", consoleRoot, new Vector3(0.9f, 0.8f, 0f));
        Box("Body", deck, new Vector3(0f, 0.06f, 0f), new Vector3(0.5f, 0.12f, 0.34f), plastic, false);
        Box("Cassette", deck, new Vector3(0.05f, 0.126f, 0.03f), new Vector3(0.2f, 0.012f, 0.13f), plasticDark, false);
        WorldText("CassetteLabel", deck, "10.17 방과후 방송 - 서진", new Vector3(0.05f, 0.134f, 0.03f), Vector3.up, 0.22f, new Vector2(0.18f, 0.04f), pen, penLit, new Color(0.85f, 0.82f, 0.7f), TextAlignmentOptions.Center, Vector3.forward);
        var playButton = Box("PlayButton", deck, new Vector3(-0.17f, 0.126f, -0.1f), new Vector3(0.06f, 0.02f, 0.05f), metal, false);
        WorldText("PlayLabel", deck, "▶ 재생", new Vector3(-0.17f, 0.124f, -0.145f), Vector3.up, 0.3f, new Vector2(0.12f, 0.03f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f), TextAlignmentOptions.Center, Vector3.forward);
        var deckCollider = deck.gameObject.AddComponent<BoxCollider>();
        deckCollider.center = new Vector3(0f, 0.07f, 0f);
        deckCollider.size = new Vector3(0.5f, 0.15f, 0.34f);
        var deckButton = deck.gameObject.AddComponent<ConsoleButton>();
        Set(deckButton, "console", console);
        Set(deckButton, "action", ConsoleButton.ButtonAction.PlayTape);
        Set(deckButton, "prompt", "테이프 재생");
        Set(deckButton, "pressPart", playButton.transform);

        // 송출 버튼
        var broadcast = Group("BroadcastButton", consoleRoot, new Vector3(1.45f, 0.8f, -0.05f));
        Box("Base", broadcast, new Vector3(0f, 0.015f, 0f), new Vector3(0.14f, 0.03f, 0.14f), metal, false);
        var red = Box("Button", broadcast, new Vector3(0f, 0.045f, 0f), new Vector3(0.08f, 0.03f, 0.08f), redButton, false);
        WorldText("BroadcastLabel", broadcast, "송 출", new Vector3(0f, 0.032f, -0.1f), Vector3.up, 0.35f, new Vector2(0.12f, 0.04f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f), TextAlignmentOptions.Center, Vector3.forward);
        var broadcastCollider = broadcast.gameObject.AddComponent<BoxCollider>();
        broadcastCollider.center = new Vector3(0f, 0.04f, 0f);
        broadcastCollider.size = new Vector3(0.18f, 0.1f, 0.18f);
        var broadcastButton = broadcast.gameObject.AddComponent<ConsoleButton>();
        Set(broadcastButton, "console", console);
        Set(broadcastButton, "action", ConsoleButton.ButtonAction.Broadcast);
        Set(broadcastButton, "prompt", "송출 버튼 누르기");
        Set(broadcastButton, "pressPart", red.transform);

        // 마이크
        Box("MicBase", consoleRoot, new Vector3(-1.5f, 0.81f, -0.15f), new Vector3(0.1f, 0.02f, 0.1f), metal, false);
        Box("MicStem", consoleRoot, new Vector3(-1.5f, 0.95f, -0.15f), new Vector3(0.015f, 0.28f, 0.015f), metal, false);
        Box("MicHead", consoleRoot, new Vector3(-1.5f, 1.1f, -0.19f), new Vector3(0.05f, 0.08f, 0.05f), black, false);

        // 벽 위 모니터 스피커 (테이프·방송 소리가 여기서 난다)
        monitorSpeaker = SpeakerBox("MonitorSpeaker", room, new Vector3(25f, 2.25f, 7.82f), new Vector3(0.32f, 0.45f, 0.14f));
        monitorSpeaker.maxDistance = 15f;
        Box("MonitorSpeaker_R", room, new Vector3(26.6f, 2.25f, 7.82f), new Vector3(0.32f, 0.45f, 0.14f), plasticDark, false);
        staticBreathSound = Emitter("Sound_StaticBreath", room, new Vector3(25.8f, 2.25f, 7.7f), SoundKind.StaticBreath, 0.9f, 20f);

        Set(console, "zoneSwitches", switches);
        Set(console, "powerLamps", lamps);
        Set(console, "monitorSpeaker", monitorSpeaker);
        Set(console, "tapeLines", content.tapeLines);
        Set(console, "tapeTranscript", content.tapeTranscript);

        // 배전함 (동쪽 벽)
        var box = Group("FuseBox", room, new Vector3(27.83f, 1.45f, 4.6f));
        var housing = Box("Housing", box, Vector3.zero, new Vector3(0.12f, 0.5f, 0.4f), metal);
        foreach (float y in new[] { -0.05f, -0.15f })
            Box("Fuse", box, new Vector3(-0.065f, y, 0f), new Vector3(0.02f, 0.025f, 0.08f), fuseMat, false);
        Box("EmptySlot", box, new Vector3(-0.062f, 0.05f, 0f), new Vector3(0.012f, 0.035f, 0.1f), plasticDark, false);
        var inserted = Box("InsertedFuse", box, new Vector3(-0.07f, 0.05f, 0f), new Vector3(0.02f, 0.025f, 0.08f), fuseMat, false);
        inserted.SetActive(false);
        WorldText("FuseLabel", box, "배전함\n<size=70%>전원</size>", new Vector3(-0.062f, 0.18f, 0f), Vector3.left, 0.5f, new Vector2(0.3f, 0.12f), gothic, gothicLit, new Color(0.85f, 0.85f, 0.8f));
        fuseBox = housing.AddComponent<FuseBox>();
        Set(fuseBox, "fuse", content.fuse);
        Set(fuseBox, "fuseVisual", inserted);

        // 서쪽 벽: 사용 수칙과 방송부 일지
        var rules = Box("RulesPoster", room, new Vector3(23.11f, 1.65f, 4.9f), new Vector3(0.01f, 0.55f, 0.42f), paper);
        WorldText("RulesTitle", room, "방송실 사용 수칙", new Vector3(23.118f, 1.85f, 4.9f), Vector3.right, 0.6f, new Vector2(0.4f, 0.08f), gothic, gothicLit, Ink);
        Readable(rules, content.broadcastRules, "수칙 읽기");
        Box("SideTable", room, new Vector3(23.45f, 0.375f, 6.3f), new Vector3(0.5f, 0.75f, 0.9f), darkWood);
        var log = Box("BroadcastLog", room, new Vector3(23.45f, 0.765f, 6.3f), new Vector3(0.22f, 0.03f, 0.3f), bookCover);
        Readable(log, content.broadcastLog, "방송부 일지 읽기", false, "10월 18일… 방송실 문 고장?");

        broadcastLight = CeilingLight("Light_Broadcast", room, new Vector3(25.5f, H, 5.5f), FlickerLight.Mode.Off, 1.8f, 6f);
        broadcastEntry = Trigger("Trigger_BroadcastEntry", room, new Vector3(24f, 1.2f, 3.8f), new Vector3(1.2f, 2.4f, 1f));
    }

    // ───────── 이벤트 연결 ─────────

    static void WireEvents()
    {
        On(flashlightPickup, "onPickedUp", playerFlashlight.Give);

        On(introTrigger, "onEnter", frontDoor.SlamShut);
        On(introTrigger, "onEnter", subtitles.SayLine, "…바람 때문인가?");

        On(flickerTrigger, "onEnter", corridorLights[1].TurnOff);

        On(classroomEntry, "onEnter", subtitles.SayLine, "우리 반 교실이다. 하나도 안 변했네.");
        On(diaryNote, "onFirstRead", chairDragTrigger.Arm);
        On(chairDragTrigger, "onEnter", chairDragSound.Play);
        On(chairDragTrigger, "onEnter", subtitles.SayLine, "…누구 있어요?");
        On(chairDragTrigger, "onEnter", boardWriting.SetActive, true);
        On(chairDragTrigger, "onEnter", classroomReentry.Arm);
        On(classroomReentry, "onEnter", stingerSound.Play);
        On(classroomReentry, "onEnter", subtitles.SayLine, "…칠판에 아까는 없던 글씨가 있다.");

        On(lockerLock, "onSolved", lockerDoor.Unlock);
        On(lockerLock, "onSolved", lockerDoor.Open);
        On(lockerLock, "onSolved", padlock.SetActive, false);

        On(musicEntry, "onEnter", piano.PlayGhostNote);
        On(musicEntry, "onEnter", subtitles.SayLine, "…방금, 피아노 소리?");
        On(piano, "onSolved", pianoDrawer.Unlock);
        On(piano, "onSolved", pianoDrawer.Open);

        On(broadcastEntry, "onEnter", subtitles.SayLine, "여기서 서진이가… 나를 기다렸구나.");
        On(fuseBox, "onPowered", console.PowerOn);
        On(fuseBox, "onPowered", broadcastLight.TurnOn);
        On(fuseBox, "onPowered", staticBreathSound.Play);

        On(console, "onBroadcastComplete", frontDoor.Unlock);
        On(console, "onBroadcastComplete", frontDoor.Open);
        On(console, "onBroadcastComplete", silhouette.SetActive, true);
        On(console, "onBroadcastComplete", subtitles.SayLine, "<color=#9EC3D9>…이번엔 네가 나갈 차례야.</color>");
        On(console, "onBroadcastComplete", subtitles.SayLine, "현관 쪽에서 문 열리는 소리가 났다.");

        On(endingTrigger, "onEnter", ending.Play);
    }

    // ───────── 공용 부품 ─────────

    static Door HingedDoor(string name, Transform parent, Vector3 hinge, float yaw, float width, float height, Material material, float openAngle)
    {
        var pivot = Group(name, parent, hinge, yaw);
        Box("Leaf", pivot, new Vector3(width / 2f, height / 2f, 0f), new Vector3(width - 0.02f, height - 0.01f, 0.06f), material);
        Box("Handle_A", pivot, new Vector3(width - 0.12f, 1f, -0.05f), new Vector3(0.12f, 0.025f, 0.025f), metal, false);
        Box("Handle_B", pivot, new Vector3(width - 0.12f, 1f, 0.05f), new Vector3(0.12f, 0.025f, 0.025f), metal, false);
        var door = pivot.gameObject.AddComponent<Door>();
        Set(door, "openAngle", openAngle);
        return door;
    }

    // 교실 미닫이문: 복도 쪽 벽면에 붙어서 옆으로 밀린다
    static Door SlidingDoor(string name, Transform parent, Vector3 center, Vector3 slide)
    {
        var root = Group(name, parent, center);
        Box("Leaf", root, new Vector3(0f, 1.05f, 0f), new Vector3(1.04f, 2.1f, 0.04f), doorWood);
        Box("Pane", root, new Vector3(0f, 1.55f, -0.022f), new Vector3(0.6f, 0.55f, 0.005f), darkGlass, false);
        Box("Handle", root, new Vector3(Mathf.Sign(slide.x) * -0.4f, 1f, -0.03f), new Vector3(0.03f, 0.15f, 0.02f), metal, false);
        var door = root.gameObject.AddComponent<Door>();
        Set(door, "sliding", true);
        Set(door, "slideOffset", slide);
        Set(door, "openDuration", 0.8f);
        return door;
    }

    static FlickerLight CeilingLight(string name, Transform parent, Vector3 position, FlickerLight.Mode mode, float intensity = 2.2f, float range = 7f, bool withLight = true)
    {
        var root = Group(name, parent, position);
        Box("Housing", root, new Vector3(0f, -0.03f, 0f), new Vector3(1.25f, 0.06f, 0.28f), metal, false);
        var tubeRenderer = Box("Tube", root, new Vector3(0f, -0.075f, 0f), new Vector3(1.15f, 0.03f, 0.08f), tube, false).GetComponent<MeshRenderer>();
        tubeRenderer.shadowCastingMode = ShadowCastingMode.Off;

        Light light = null;
        AudioSource buzz = null;
        if (withLight)
        {
            light = PointLight("Light", root, new Vector3(0f, -0.3f, 0f), new Color(0.8f, 0.9f, 1f), intensity * CeilingLightBoost, range);
            buzz = Speaker(root.gameObject, 9f);
            buzz.volume = 0.12f;
            buzz.minDistance = 1f;
            buzz.loop = true;
        }
        var flicker = root.gameObject.AddComponent<FlickerLight>();
        Set(flicker, "lightSource", light);
        Set(flicker, "tube", tubeRenderer);
        Set(flicker, "mode", mode);
        Set(flicker, "buzz", buzz);
        return flicker;
    }

    static AudioSource SpeakerBox(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var box = Box(name, parent, position, size, plastic, false);
        var source = Speaker(box);
        source.volume = 0.9f;
        return source;
    }

    static TriggerZone Trigger(string name, Transform parent, Vector3 center, Vector3 size, bool armed = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        var zone = go.AddComponent<TriggerZone>();
        Set(zone, "armed", armed);
        return zone;
    }

    static SoundEmitter Emitter(string name, Transform parent, Vector3 position, SoundKind kind, float volume, float maxDistance)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var emitter = go.AddComponent<SoundEmitter>();
        Set(emitter, "sound", kind);
        Set(emitter, "volume", volume);
        Set(emitter, "maxDistance", maxDistance);
        return emitter;
    }

    static ReadableNote Readable(GameObject host, DocumentData document, string prompt, bool take = false, string thought = null)
    {
        var note = host.AddComponent<ReadableNote>();
        Set(note, "document", document);
        Set(note, "prompt", prompt);
        Set(note, "takeAfterReading", take);
        if (thought != null) Set(note, "thoughtAfterReading", thought);
        UsedText.Append(prompt).Append(thought);
        return note;
    }

    static void Examine(GameObject host, params string[] lines)
    {
        Set(host.AddComponent<Examinable>(), "lines", lines);
        foreach (string line in lines) UsedText.Append(line);
    }

    static PickupItem Pickup(GameObject host, ItemData item, Vector3 colliderSize)
    {
        var pickup = host.AddComponent<PickupItem>();
        Set(pickup, "item", item);
        host.GetComponent<BoxCollider>().size = colliderSize;
        return pickup;
    }
}
