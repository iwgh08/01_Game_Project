using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// 씬 생성기 공용 도구: 상자 메시, 벽, 재질, 절차적 텍스처, 월드 글씨, 직렬화 필드 설정, UnityEvent 연결
public static class BuilderKit
{
    public const string ArtFolder = "Assets/Art";

    // 씬에 쓰인 글자를 모아 두었다가 폰트 아틀라스에 미리 넣는다
    public static readonly StringBuilder UsedText = new();
    static readonly Dictionary<string, Mesh> meshCache = new();

    public static void Begin()
    {
        meshCache.Clear();
        UsedText.Clear();
        EnsureFolder(ArtFolder + "/Materials");
        EnsureFolder(ArtFolder + "/Textures");
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    public static Transform Group(string name, Transform parent, Vector3 localPosition = default, float yaw = 0f)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPosition;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return t;
    }

    // ───────── 메시 ─────────

    // 크기에 맞춰 UV를 미터 단위로 펴 준 상자 (텍스처가 늘어나지 않는다)
    public static GameObject Box(string name, Transform parent, Vector3 localCenter, Vector3 size, Material material, bool collider = true, float uvTile = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localCenter;
        go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, uvTile);
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    static Mesh BoxMesh(Vector3 size, float uvTile)
    {
        string key = $"{size.x:F3}_{size.y:F3}_{size.z:F3}_{uvTile:F2}";
        if (meshCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var vertices = new List<Vector3>(24);
        var normals = new List<Vector3>(24);
        var uvs = new List<Vector2>(24);
        var triangles = new List<int>(36);
        Vector3 half = size / 2f;
        Vector3[] faces = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var n in faces)
        {
            Vector3 v = Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up;
            Vector3 u = Vector3.Cross(n, v);
            Vector3 c = Vector3.Scale(n, half);
            float hu = Mathf.Abs(Vector3.Dot(u, half)), hv = Mathf.Abs(Vector3.Dot(v, half));
            int start = vertices.Count;
            foreach (var p in new[] { c - u * hu - v * hv, c - u * hu + v * hv, c + u * hu + v * hv, c + u * hu - v * hv })
            {
                vertices.Add(p);
                normals.Add(n);
                uvs.Add(new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, v)) / uvTile);
            }
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        var mesh = new Mesh { name = "Box_" + key };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        meshCache[key] = mesh;
        return mesh;
    }

    // ───────── 벽 ─────────

    public readonly struct Opening
    {
        public readonly float center, width, bottom, top;

        public Opening(float center, float width, float bottom, float top)
        {
            this.center = center;
            this.width = width;
            this.bottom = bottom;
            this.top = top;
        }

        public static Opening Door(float center, float width = 1f, float height = 2.1f) => new(center, width, 0f, height);
        public static Opening Window(float center, float width = 1.6f) => new(center, width, 1f, 2.5f);
    }

    // a→b 직선 벽 (x축 또는 z축 방향). 구멍(문·창문)은 a에서부터의 거리로 지정한다.
    // 아래쪽(wainscot 높이까지)과 위쪽 재질을 나눈다.
    public static Transform Wall(string name, Transform parent, Vector3 a, Vector3 b, float height, float thickness, float wainscot,
        Material lower, Material upper, params Opening[] openings)
    {
        var root = Group(name, parent);
        Vector3 dir = (b - a).normalized;
        float ext = thickness / 2f; // 모서리가 비지 않게 양 끝을 두께 절반만큼 늘린다
        Vector3 start = a - dir * ext;
        float total = Vector3.Distance(a, b) + ext * 2f;
        bool alongX = Mathf.Abs(dir.x) > 0.5f;

        void Piece(float s0, float s1, float y0, float y1, Material material)
        {
            if (s1 - s0 < 0.001f || y1 - y0 < 0.001f) return;
            Vector3 center = start + dir * ((s0 + s1) / 2f) + Vector3.up * ((y0 + y1) / 2f);
            Vector3 size = alongX ? new Vector3(s1 - s0, y1 - y0, thickness) : new Vector3(thickness, y1 - y0, s1 - s0);
            Box("Wall", root, center, size, material, true, 2f);
        }

        void Span(float s0, float s1, float y0, float y1)
        {
            if (y0 < wainscot && y1 > wainscot)
            {
                Piece(s0, s1, y0, wainscot, lower);
                Piece(s0, s1, wainscot, y1, upper);
            }
            else Piece(s0, s1, y0, y1, y1 <= wainscot ? lower : upper);
        }

        var sorted = new List<Opening>(openings);
        sorted.Sort((p, q) => p.center.CompareTo(q.center));
        float cursor = 0f;
        foreach (var o in sorted)
        {
            float s0 = o.center + ext - o.width / 2f, s1 = o.center + ext + o.width / 2f;
            Span(cursor, s0, 0f, height);
            Span(s0, s1, 0f, o.bottom);
            Span(s0, s1, o.top, height);
            cursor = s1;
        }
        Span(cursor, total, 0f, height);
        return root;
    }

    // ───────── 재질·텍스처 ─────────

    public static Material Mat(string name, Color color, float smoothness = 0.15f, float metallic = 0f, Texture2D texture = null, Color? emission = null)
    {
        string path = $"{ArtFolder}/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetTexture("_BaseMap", texture);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Material GlassMat(string name, Color color)
    {
        var mat = Mat(name, color, 0.95f);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetShaderPassEnabled("DepthOnly", false);
        mat.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // 흑백 절차적 텍스처를 PNG로 저장한다 (재질 색과 곱해서 쓴다)
    public static Texture2D Texture(string name, int size, Func<float, float, float> shade)
    {
        string path = $"{ArtFolder}/Textures/{name}.png";
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            byte value = (byte)(Mathf.Clamp01(shade(x / (float)size, y / (float)size)) * 255f);
            pixels[y * size + x] = new Color32(value, value, value, 255);
        }
        tex.SetPixels32(pixels);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // 이음새 없이 반복되는 펄린 노이즈 (0~1 UV 기준)
    public static float TileNoise(float u, float v, float freqU, float freqV, float seed)
    {
        float a = Mathf.PerlinNoise(seed + u * freqU, seed + v * freqV);
        float b = Mathf.PerlinNoise(seed + (u - 1f) * freqU, seed + v * freqV);
        float c = Mathf.PerlinNoise(seed + u * freqU, seed + (v - 1f) * freqV);
        float d = Mathf.PerlinNoise(seed + (u - 1f) * freqU, seed + (v - 1f) * freqV);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    public static float Hash(float x, float y) => Frac(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f);
    public static float Frac(float x) => x - Mathf.Floor(x);

    // ───────── 글씨 ─────────

    // TMP 글씨가 조명을 받도록 URP Lit 셰이더로 바꾼 재질
    public static Material LitTextMaterial(TMP_FontAsset font, string name)
    {
        string path = $"{ArtFolder}/Materials/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
        var mat = new Material(font.material) { name = name };
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-URP Lit.shadergraph");
        if (shader != null) mat.shader = shader;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // facing: 글씨 앞면이 향하는 방향 (읽는 사람 쪽). 바닥에 눕힌 글씨는 up으로 글자 위쪽 방향을 정한다.
    public static TextMeshPro WorldText(string name, Transform parent, string text, Vector3 position, Vector3 facing, float fontSize, Vector2 size,
        TMP_FontAsset font, Material material, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, Vector3? up = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.LookRotation(-facing, up ?? Vector3.up);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.fontSharedMaterial = material;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.rectTransform.sizeDelta = size;
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        UsedText.Append(text);
        return tmp;
    }

    // ───────── 빛·소리 ─────────

    public static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range, LightShadows shadows = LightShadows.None)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = shadows;
        return light;
    }

    public static AudioSource Speaker(GameObject host, float maxDistance = 25f)
    {
        var source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        return source;
    }

    // ───────── 후처리 ─────────

    public static VolumeProfile HorrorVolumeProfile()
    {
        const string path = "Assets/Settings/HorrorVolumeProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        foreach (var old in profile.components)
        {
            AssetDatabase.RemoveObjectFromAsset(old);
            Object.DestroyImmediate(old, true);
        }
        profile.components.Clear();

        AddOverride<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);
        var color = AddOverride<ColorAdjustments>(profile);
        color.postExposure.Override(0.9f);
        color.contrast.Override(14f);
        color.saturation.Override(-38f);
        color.colorFilter.Override(new Color(0.88f, 0.94f, 1f));
        var vignette = AddOverride<Vignette>(profile);
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.5f);
        var grain = AddOverride<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Medium3);
        grain.intensity.Override(0.38f);
        grain.response.Override(0.75f);
        AddOverride<ChromaticAberration>(profile).intensity.Override(0.12f);
        var bloom = AddOverride<Bloom>(profile);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.65f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    static T AddOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        var component = profile.Add<T>(false);
        component.name = typeof(T).Name;
        component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    // ───────── 직렬화 필드·이벤트 ─────────

    // private [SerializeField] 필드에 값을 넣는다
    public static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field) ?? throw new ArgumentException($"{target.GetType().Name}에 '{field}' 필드가 없다");
        Assign(property, value);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Assign(SerializedProperty property, object value)
    {
        switch (value)
        {
            case null: property.objectReferenceValue = null; break;
            case Object o: property.objectReferenceValue = o; break;
            case bool b: property.boolValue = b; break;
            case int i: property.intValue = i; break;
            case float f: property.floatValue = f; break;
            case string s: property.stringValue = s; break;
            case Color c: property.colorValue = c; break;
            case Vector2 v2: property.vector2Value = v2; break;
            case Vector3 v3: property.vector3Value = v3; break;
            case Enum e: property.intValue = Convert.ToInt32(e); break;
            case Array array:
                property.arraySize = array.Length;
                for (int k = 0; k < array.Length; k++) Assign(property.GetArrayElementAtIndex(k), array.GetValue(k));
                break;
            default: throw new ArgumentException($"지원하지 않는 형식: {value.GetType()}");
        }
    }

    static UnityEventBase GetEvent(Object target, string field)
    {
        var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   ?? throw new ArgumentException($"{target.GetType().Name}에 '{field}' 이벤트가 없다");
        return (UnityEventBase)info.GetValue(target);
    }

    // 인스펙터에서도 보이는 영구 리스너로 연결한다
    public static void On(Object target, string eventField, UnityAction action)
    {
        UnityEventTools.AddPersistentListener((UnityEvent)GetEvent(target, eventField), action);
        EditorUtility.SetDirty(target);
    }

    public static void On(Object target, string eventField, UnityAction<string> action, string argument)
    {
        UnityEventTools.AddStringPersistentListener(GetEvent(target, eventField), action, argument);
        EditorUtility.SetDirty(target);
    }

    public static void On(Object target, string eventField, UnityAction<bool> action, bool argument)
    {
        UnityEventTools.AddBoolPersistentListener(GetEvent(target, eventField), action, argument);
        EditorUtility.SetDirty(target);
    }
}
