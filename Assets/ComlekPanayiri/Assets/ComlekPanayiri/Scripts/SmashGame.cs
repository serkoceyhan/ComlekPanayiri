using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// ÇÖMLEK PANAYIRI — ana oyun scripti.
// Sahneye hiçbir şey eklemen gerekmez: Play'e basınca kendini kurar
// (kamera, ışık, panayır, top, bölümler, arayüz, sesler).
public class SmashGame : MonoBehaviour
{
    public static SmashGame Instance;
    public const float JarBreakSpeed = 3.4f;   // çömleğin kırılması için gereken çarpma hızı
    const float G = 12f;                        // yerçekimi (biraz yüksek: daha "oyunsu" hissettirir)

    enum Phase { Menu, Play, WinPending, Won, Lost }

    static Color Hex(int h) => new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f);
    static readonly Color Ink = Hex(0x1d1424), Paper = Hex(0xfff6e6), Brass = Hex(0xf2b544),
        BrassDark = Hex(0xb07c1c), Chili = Hex(0xe2483d), ChiliDark = Hex(0x9b2a22), Teal = Hex(0x2f8f88),
        Dusk = Hex(0xe9a070);
    static readonly Color[] JarColors =
        { Hex(0xe2483d), Hex(0x2f8f88), Hex(0xf2b544), Hex(0x3f6fb5), Hex(0xd96aa7), Hex(0x7bb661) };

    // --- sahne ---
    Camera cam;
    [Header("Sahne (Çömlek Panayırı > Sahneyi Kur menüsü doldurur)")]
    public Camera sceneCam;
    public Transform sceneYaw, scenePitch, sceneBarrel;
    public Light sceneFlash;
    public Transform decorRoot, cannonRoot, levelsRoot;

    Light flash;
    Transform yaw, pitch, barrel;
    GameObject levelRoot;
    LevelInfo[] sceneLevels;
    bool Authored => sceneCam != null && sceneYaw != null && scenePitch != null && sceneBarrel != null;
    readonly List<Target> targets = new List<Target>();
    Transform[] dots;
    AudioSource[] voices;
    int voiceIdx;
    Mesh jarMesh, jarCol;
    Material cloth, skirt, woodDark, wood, stoneMat, stoneDownMat, cream, iron, brassMat, smokeMat, dotMat;
    Material[] jarMats, bulbMats;

    // --- oyun durumu ---
    Phase phase = Phase.Menu;
    int level, ballsLeft, unlocked = 1, earned;
    int[] stars;
    float lastShot = -99f, phaseAt, armedAt, recoil, shake, flashT, aimT;
    bool aiming, hasAim, muted;
    Vector3 aimVel;
    readonly List<Rect> uiRects = new List<Rect>();

    public bool Armed => Time.time > armedAt;
    Vector3 PivotPos => pitch.position;

    // ================================================================ kurulum

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (Instance != null) return;
        new GameObject("ComlekPanayiri").AddComponent<SmashGame>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Physics.gravity = new Vector3(0, -G, 0);
        Time.fixedDeltaTime = 0.01f;
        Physics.defaultSolverIterations = 10;
        Physics.IgnoreLayerCollision(2, 2, true); // gülle/parça/konfeti birbirine çarpmasın
        Application.targetFrameRate = 60;

        Sfx.Build();
        Proc.Warmup();
        BuildMaterials();
        if (Authored)
        {
            // Sahne editörde kurulmuş: hazır objeleri kullan.
            cam = sceneCam; yaw = sceneYaw; pitch = scenePitch; barrel = sceneBarrel; flash = sceneFlash;
            DisableSceneDefaults();
            if (flash == null)
            {
                flash = new GameObject("NamluAlevi").AddComponent<Light>();
                flash.type = LightType.Point; flash.color = Brass; flash.range = 8f;
                flash.transform.SetParent(barrel, false);
                flash.transform.localPosition = new Vector3(0, 0, 1.8f);
            }
            flash.intensity = 0f;
            AimAt(new Vector3(0, 1.6f, 4f));
        }
        else
        {
            DisableSceneDefaults();
            BuildWorld();
            BuildCannon();
        }
        ResolveSceneLevels();
        BuildDots();
        BuildAudio();
        LoadProgress();
        ShowMenu();
    }

    static T[] FindAll<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>();
#endif
    }

    // Sahnedeki varsayılan kamera ve güneşi kapatır (bizimkilere dokunmaz).
    public void DisableSceneDefaults()
    {
        foreach (var c in FindAll<Camera>())
            if (c != sceneCam && c != cam) c.gameObject.SetActive(false);
        if (Authored) return;
        foreach (var l in FindAll<Light>())
            if (l.type == LightType.Directional && (decorRoot == null || !l.transform.IsChildOf(decorRoot)))
                l.gameObject.SetActive(false);
    }

    // ---- bölüm kaynağı: sahnedeki "Bolumler" altı varsa onu, yoksa Levels.cs'i kullan
    void ResolveSceneLevels()
    {
        sceneLevels = null;
        if (levelsRoot == null) return;
        var list = new List<LevelInfo>();
        foreach (Transform child in levelsRoot)
        {
            var info = child.GetComponent<LevelInfo>();
            if (info == null) continue;
            child.gameObject.SetActive(false); // şablon: oyun sırasında kopyası kullanılır
            list.Add(info);
        }
        if (list.Count > 0) sceneLevels = list.ToArray();
    }

    int LevelCount => sceneLevels != null ? sceneLevels.Length : Levels.All.Length;
    string LevelName(int i) => sceneLevels != null ? sceneLevels[i].levelName : Levels.All[i].name;
    int LevelBalls(int i) => Mathf.Max(1, sceneLevels != null ? sceneLevels[i].balls : Levels.All[i].balls);

    Transform DecorRoot
    {
        get
        {
            if (decorRoot == null) decorRoot = new GameObject("Panayir").transform;
            return decorRoot;
        }
    }

    void BuildMaterials()
    {
        cloth = Proc.Mat(Color.white, 0.2f, 0, 0, Proc.Stripes(Chili, Paper), new Vector2(3, 1));
        skirt = Proc.Mat(Teal, 0.2f);
        woodDark = Proc.Mat(Hex(0x5a3a26), 0.15f);
        wood = Proc.Mat(Hex(0xb07a45), 0.3f);
        stoneMat = Proc.Mat(Hex(0x8c8893), 0.1f);
        stoneDownMat = Proc.Mat(Hex(0x4a4650), 0.05f);
        cream = Proc.Mat(Paper, 0.5f);
        iron = Proc.Mat(Hex(0x2b2730), 0.6f, 0.7f);
        brassMat = Proc.Mat(Brass, 0.7f, 0.8f);
        smokeMat = Proc.Mat(Hex(0xd8cfc8), 0f);
        dotMat = Proc.Mat(Paper, 0f, 0f, 1.2f);

        jarMats = new Material[JarColors.Length];
        for (int i = 0; i < JarColors.Length; i++) jarMats[i] = Proc.Mat(JarColors[i], 0.7f);
        bulbMats = new[]
        {
            Proc.Mat(Hex(0xfff0b0), 0.5f, 0f, 2.5f), Proc.Mat(Hex(0xffb35c), 0.5f, 0f, 2.5f),
            Proc.Mat(Hex(0xfff0b0), 0.5f, 0f, 2.5f), Proc.Mat(Hex(0xff6f61), 0.5f, 0f, 2.5f),
        };

        jarMesh = Proc.Lathe(new[]
        {
            new Vector2(0f, -0.4f), new Vector2(0.2f, -0.4f), new Vector2(0.3f, -0.3f), new Vector2(0.35f, -0.1f),
            new Vector2(0.33f, 0.08f), new Vector2(0.24f, 0.24f), new Vector2(0.15f, 0.32f), new Vector2(0.15f, 0.36f),
            new Vector2(0.2f, 0.4f), new Vector2(0.16f, 0.4f), new Vector2(0f, 0.38f),
        }, 24);
        jarCol = Proc.Lathe(new[]
        {
            new Vector2(0f, -0.4f), new Vector2(0.3f, -0.4f), new Vector2(0.3f, 0.4f), new Vector2(0f, 0.4f),
        }, 10);
    }

    void BuildWorld()
    {
        var cg = new GameObject("Kamera");
        cg.tag = "MainCamera";
        cam = cg.AddComponent<Camera>();
        cg.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Dusk;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Dusk;
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 90f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Hex(0xffe2c0) * 0.9f;
        RenderSettings.ambientEquatorColor = Hex(0xd08a6a) * 0.7f;
        RenderSettings.ambientGroundColor = Hex(0x3a2a40);

        var sun = new GameObject("Gunes").AddComponent<Light>();
        sun.transform.SetParent(DecorRoot, false);
        sun.type = LightType.Directional;
        sun.color = Hex(0xffd6a0);
        sun.intensity = 1.3f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.75f;
        sun.transform.rotation = Quaternion.Euler(42f, 25f, 0f);

        // zemin (kum)
        Proc.Prim(PrimitiveType.Plane, DecorRoot, Vector3.zero, new Vector3(16, 1, 16), Proc.Mat(Hex(0xd9b98a), 0.1f), true)
            .name = "Zemin";

        // çizgili panayır çadırı (her yanı saran duvar)
        var tent = new GameObject("Cadir");
        tent.transform.SetParent(DecorRoot, false);
        tent.transform.position = new Vector3(0, 0, 2);
        tent.AddComponent<MeshFilter>().sharedMesh =
            Proc.Lathe(new[] { new Vector2(30f, 0f), new Vector2(30f, 16f) }, 64, true);
        var tr = tent.AddComponent<MeshRenderer>();
        tr.sharedMaterial = Proc.Mat(Color.white, 0.1f, 0, 0, Proc.Stripes(Chili, Paper), new Vector2(24, 1));
        tr.shadowCastingMode = ShadowCastingMode.Off;

        // direkler + ampul dizileri + flamalar
        var poles = new[] { new Vector3(-10, 0, -4), new Vector3(10, 0, -4), new Vector3(-10, 0, 12), new Vector3(10, 0, 12) };
        foreach (var p in poles)
            Proc.Prim(PrimitiveType.Cylinder, DecorRoot, p + Vector3.up * 4f, new Vector3(0.18f, 4f, 0.18f), woodDark, false);
        Strand(new Vector3(-10, 8, -4), new Vector3(10, 8, -4), 1.4f, 18, false);
        Strand(new Vector3(-10, 8, 12), new Vector3(10, 8, 12), 1.6f, 18, false);
        Strand(new Vector3(-10, 7.2f, 12), new Vector3(10, 7.2f, 12), 1.2f, 16, true);
        Strand(new Vector3(-10, 8, -4), new Vector3(-10, 8, 12), 1f, 12, false);
        Strand(new Vector3(10, 8, -4), new Vector3(10, 8, 12), 1f, 12, false);
    }

    void Strand(Vector3 a, Vector3 b, float sag, int n, bool flags)
    {
        Vector3 prev = a;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            var p = Vector3.Lerp(a, b, t);
            p.y -= sag * 4f * t * (1f - t);
            if (i > 0)
            {
                var wire = Proc.Prim(PrimitiveType.Cube, DecorRoot, (prev + p) * 0.5f,
                    new Vector3(0.03f, 0.03f, Vector3.Distance(prev, p)), woodDark, false);
                wire.transform.rotation = Quaternion.LookRotation(p - prev);
                wire.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (i > 0 && i < n)
            {
                GameObject g;
                if (flags)
                {
                    g = Proc.Prim(PrimitiveType.Cube, DecorRoot, p + Vector3.down * 0.3f, new Vector3(0.4f, 0.4f, 0.02f),
                        jarMats[i % jarMats.Length], false);
                    g.transform.rotation = Quaternion.Euler(0, 0, 45);
                }
                else
                {
                    g = Proc.Prim(PrimitiveType.Sphere, DecorRoot, p + Vector3.down * 0.12f, Vector3.one * 0.16f,
                        bulbMats[i % bulbMats.Length], false);
                }
                g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            prev = p;
        }
    }

    void BuildCannon()
    {
        var root = new GameObject("Top");
        cannonRoot = root.transform;
        root.transform.position = new Vector3(0, 0, -11);
        yaw = new GameObject("Donus").transform;
        yaw.SetParent(root.transform, false);

        Proc.Prim(PrimitiveType.Cube, yaw, new Vector3(0, 0.55f, -0.1f), new Vector3(1.0f, 0.5f, 1.5f), wood, false);
        foreach (float sx in new[] { -1f, 1f })
        {
            Proc.Prim(PrimitiveType.Cylinder, yaw, new Vector3(sx * 0.62f, 0.55f, -0.1f), new Vector3(1.1f, 0.08f, 1.1f),
                woodDark, false).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Proc.Prim(PrimitiveType.Cylinder, yaw, new Vector3(sx * 0.69f, 0.55f, -0.1f), new Vector3(0.3f, 0.05f, 0.3f),
                brassMat, false).transform.localRotation = Quaternion.Euler(0, 0, 90);
        }

        pitch = new GameObject("Egim").transform;
        pitch.SetParent(yaw, false);
        pitch.localPosition = new Vector3(0, 1.15f, 0);
        barrel = new GameObject("Namlu").transform;
        barrel.SetParent(pitch, false);
        Proc.Prim(PrimitiveType.Cylinder, barrel, new Vector3(0, 0, 0.55f), new Vector3(0.62f, 0.95f, 0.62f), iron, false)
            .transform.localRotation = Quaternion.Euler(90, 0, 0);
        Proc.Prim(PrimitiveType.Cylinder, barrel, new Vector3(0, 0, 1.45f), new Vector3(0.74f, 0.06f, 0.74f), brassMat, false)
            .transform.localRotation = Quaternion.Euler(90, 0, 0);
        Proc.Prim(PrimitiveType.Cylinder, barrel, new Vector3(0, 0, -0.3f), new Vector3(0.72f, 0.08f, 0.72f), brassMat, false)
            .transform.localRotation = Quaternion.Euler(90, 0, 0);
        Proc.Prim(PrimitiveType.Sphere, barrel, new Vector3(0, 0, -0.45f), Vector3.one * 0.55f, iron, false);

        flash = new GameObject("NamluAlevi").AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = Brass;
        flash.range = 8f;
        flash.intensity = 0f;
        flash.transform.SetParent(barrel, false);
        flash.transform.localPosition = new Vector3(0, 0, 1.8f);

        AimAt(new Vector3(0, 1.6f, 4f));
    }

    void BuildDots()
    {
        dots = new Transform[24];
        var dotRoot = new GameObject("NisanNoktalari").transform;
        for (int i = 0; i < dots.Length; i++)
        {
            var d = Proc.Prim(PrimitiveType.Sphere, dotRoot, Vector3.zero, Vector3.one * 0.14f, dotMat, false);
            d.layer = 2;
            d.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            d.SetActive(false);
            dots[i] = d.transform;
        }
    }

    void BuildAudio()
    {
        voices = new AudioSource[8];
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
        }
    }

    public void PlaySfx(AudioClip clip, float vol, float pitchMul)
    {
        if (clip == null || voices == null) return;
        var v = voices[voiceIdx++ % voices.Length];
        v.pitch = pitchMul;
        v.PlayOneShot(clip, vol);
    }

    // ================================================================ bölüm kurucuları (Levels.cs kullanır)

    Transform Root => levelRoot != null ? levelRoot.transform : null;

    public void Table(float x, float z, float w, float d, float h, float moveAmp = 0f, float moveSpeed = 0f)
    {
        var t = new GameObject("Masa");
        t.transform.SetParent(Root, false);
        t.transform.position = new Vector3(x, h, z);
        Proc.Prim(PrimitiveType.Cube, t.transform, new Vector3(0, -0.1f, 0), new Vector3(w, 0.2f, d), cloth, true);
        Proc.Prim(PrimitiveType.Cube, t.transform, new Vector3(0, -0.4f, -d / 2f - 0.01f), new Vector3(w, 0.45f, 0.02f), skirt, false);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Proc.Prim(PrimitiveType.Cylinder, t.transform,
                    new Vector3(sx * (w / 2f - 0.15f), -h / 2f, sz * (d / 2f - 0.15f)), new Vector3(0.12f, h / 2f, 0.12f),
                    woodDark, false);
        if (moveAmp > 0f)
        {
            var rb = t.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var mp = t.AddComponent<MovingPlatform>();
            mp.amp = moveAmp;
            mp.speed = moveSpeed;
        }
    }

    public void Jar(float x, float yBottom, float z, int color = -1)
    {
        int ci = color >= 0 ? color % JarColors.Length : targets.Count % JarColors.Length;
        var go = new GameObject("Comlek");
        go.transform.SetParent(Root, false);
        go.transform.position = new Vector3(x, yBottom + 0.405f, z);
        go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        go.AddComponent<MeshFilter>().sharedMesh = jarMesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = jarMats[ci];
        Proc.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, -0.08f, 0), new Vector3(0.72f, 0.03f, 0.72f), cream, false);
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = jarCol;
        mc.convex = true;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.6f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var tg = go.AddComponent<Target>();
        tg.Init(this, true, JarColors[ci]);
        targets.Add(tg);
    }

    public void Stone(float x, float yBottom, float z)
    {
        var go = Proc.Prim(PrimitiveType.Cube, Root, Vector3.zero, Vector3.one * 0.7f, stoneMat, true);
        go.name = "Tas";
        go.transform.position = new Vector3(x, yBottom + 0.355f, z);
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 3.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var tg = go.AddComponent<Target>();
        tg.Init(this, false, Color.gray);
        targets.Add(tg);
    }

    public void Plank(float x, float yBottom, float z, float len)
    {
        var go = Proc.Prim(PrimitiveType.Cube, Root, Vector3.zero, new Vector3(len, 0.15f, 0.7f), wood, true);
        go.name = "Tahta";
        go.transform.position = new Vector3(x, yBottom + 0.08f, z);
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    // ================================================================ editör aracı için

    // Çömlek Panayırı > Sahneyi Kur menüsü bunu çağırır (Play'e basmadan, editörde).
    public void BuildForEditor()
    {
        DisableSceneDefaults();
        Proc.Warmup();
        BuildMaterials();
        BuildWorld();
        BuildCannon();
        sceneCam = cam; sceneYaw = yaw; scenePitch = pitch; sceneBarrel = barrel; sceneFlash = flash;
        levelsRoot = new GameObject("Bolumler").transform;
        for (int i = 0; i < Levels.All.Length; i++)
        {
            targets.Clear();
            levelRoot = new GameObject("Bolum_" + (i + 1));
            var info = levelRoot.AddComponent<LevelInfo>();
            info.levelName = Levels.All[i].name;
            info.balls = Levels.All[i].balls;
            Levels.All[i].build(this);
            levelRoot.transform.SetParent(levelsRoot, true);
            levelRoot.SetActive(i == 0);
        }
        levelRoot = null;
        targets.Clear();
        FitCamera();
    }

    // Editör aracı parça prefab'ları üretirken parçaları bu objenin altına koyar.
    public void SetBuildRoot(GameObject root) { levelRoot = root; }

    // ================================================================ oyun akışı

    void BuildLevel(int i)
    {
        if (levelRoot != null) { levelRoot.SetActive(false); Destroy(levelRoot); }
        targets.Clear();
        if (sceneLevels != null)
        {
            levelRoot = Instantiate(sceneLevels[i].gameObject);
            levelRoot.name = "Oynanan_" + sceneLevels[i].name;
            levelRoot.transform.SetParent(null, true);
            levelRoot.SetActive(true);
            foreach (var t in levelRoot.GetComponentsInChildren<Target>(true))
            {
                t.Init(this, t.isJar, t.color);
                targets.Add(t);
            }
        }
        else
        {
            levelRoot = new GameObject("Bolum_" + (i + 1));
            Levels.All[i].build(this);
        }
        armedAt = Time.time + 0.8f; // ilk anlarda yerleşme sarsıntısı çömlek kırmasın
    }

    void StartLevel(int i)
    {
        level = Mathf.Clamp(i, 0, LevelCount - 1);
        BuildLevel(level);
        ballsLeft = LevelBalls(level);
        lastShot = Time.time;
        aiming = false;
        phase = Phase.Play;
        AimAt(new Vector3(0, 1.6f, 4f));
        PlaySfx(Sfx.Click, 0.6f, 1f);
    }

    void ShowMenu()
    {
        phase = Phase.Menu;
        level = Mathf.Clamp(unlocked - 1, 0, LevelCount - 1);
        BuildLevel(level);
    }

    int Remaining()
    {
        int n = 0;
        foreach (var t in targets) if (!t.done) n++;
        return n;
    }

    public void OnJarBroken(Target t)
    {
        var p = t.transform.position;
        var jm = t.GetComponent<Renderer>().sharedMaterial;
        PlaySfx(Sfx.Crash, 0.8f, Random.Range(0.85f, 1.25f));
        shake = Mathf.Max(shake, 0.18f);

        for (int i = 0; i < 8; i++)
        {
            var s = Proc.Prim(PrimitiveType.Cube, Root, Vector3.zero, Vector3.one * Random.Range(0.09f, 0.2f),
                i % 3 == 0 ? cream : jm, true);
            s.transform.position = p + Random.insideUnitSphere * 0.3f;
            s.transform.rotation = Random.rotation;
            s.layer = 2;
            var rb = s.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            Proc.SetVel(rb, Random.insideUnitSphere * 4f + Vector3.up * 3f);
            s.AddComponent<AutoFade>().life = Random.Range(1.6f, 2.6f);
        }
        for (int i = 0; i < 12; i++)
        {
            var c = Proc.Prim(PrimitiveType.Cube, Root, Vector3.zero, new Vector3(0.12f, 0.02f, 0.08f),
                i % 4 == 0 ? brassMat : jarMats[Random.Range(0, jarMats.Length)], false);
            c.transform.position = p;
            c.layer = 2;
            c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var f = c.AddComponent<AutoFade>();
            f.manual = true;
            f.vel = Random.insideUnitSphere * 3f + Vector3.up * 4f;
            f.spin = Random.insideUnitSphere * 500f;
            f.gravityScale = 0.3f;
            f.life = 1.8f;
        }
    }

    public void OnStoneDown(Target t)
    {
        PlaySfx(Sfx.Thud, 0.8f, Random.Range(0.7f, 0.9f));
        var r = t.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = stoneDownMat;
    }

    void Update()
    {
        FitCamera();
        HandleInput();
        AnimateCannon();
        UpdatePhase();
    }

    void UpdatePhase()
    {
        if (phase == Phase.Play)
        {
            if (Armed && Remaining() == 0)
            {
                phase = Phase.WinPending;
                phaseAt = Time.time + 1.1f;
            }
            else if (ballsLeft == 0 && Time.time - lastShot > 4f)
            {
                phase = Phase.Lost;
                PlaySfx(Sfx.Lose, 0.7f, 1f);
            }
        }
        else if (phase == Phase.WinPending && Time.time > phaseAt)
        {
            SaveWin();
            phase = Phase.Won;
            PlaySfx(Sfx.Win, 0.8f, 1f);
        }
    }

    // ================================================================ nişan + atış

    Vector3 SolveVel(Vector3 target, out float flightTime)
    {
        var d = target - PivotPos;
        float horiz = new Vector2(d.x, d.z).magnitude;
        flightTime = Mathf.Clamp(0.45f + horiz * 0.045f, 0.5f, 1.4f);
        var v = d / flightTime;
        v.y += 0.5f * G * flightTime;
        return v;
    }

    void AimAt(Vector3 target)
    {
        aimVel = SolveVel(target, out aimT);
        float h = new Vector2(aimVel.x, aimVel.z).magnitude;
        yaw.localRotation = Quaternion.Euler(0, Mathf.Atan2(aimVel.x, aimVel.z) * Mathf.Rad2Deg, 0);
        pitch.localRotation = Quaternion.Euler(-Mathf.Atan2(aimVel.y, h) * Mathf.Rad2Deg, 0, 0);
        hasAim = true;
    }

    bool TryAimPoint(Vector2 screen, out Vector3 p)
    {
        var ray = cam.ScreenPointToRay(screen);
        if (Physics.Raycast(ray, out var hit, 200f, ~(1 << 2), QueryTriggerInteraction.Ignore) && hit.point.z > -4f)
        {
            p = hit.point;
            return true;
        }
        var plane = new Plane(Vector3.back, new Vector3(0, 0, 4));
        if (plane.Raycast(ray, out float e))
        {
            p = ray.GetPoint(e);
            p.y = Mathf.Max(p.y, 0.3f);
            return true;
        }
        p = default;
        return false;
    }

    void ReadPointer(out Vector2 pos, out bool pressed, out bool released, out bool held, out bool isMouse)
    {
        pos = default; pressed = released = held = false; isMouse = true;
#if ENABLE_INPUT_SYSTEM
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && (ts.primaryTouch.press.isPressed || ts.primaryTouch.press.wasReleasedThisFrame))
        {
            var t = ts.primaryTouch;
            pos = t.position.ReadValue();
            pressed = t.press.wasPressedThisFrame;
            released = t.press.wasReleasedThisFrame;
            held = t.press.isPressed;
            isMouse = false;
            return;
        }
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m != null)
        {
            pos = m.position.ReadValue();
            pressed = m.leftButton.wasPressedThisFrame;
            released = m.leftButton.wasReleasedThisFrame;
            held = m.leftButton.isPressed;
        }
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            pos = t.position;
            pressed = t.phase == TouchPhase.Began;
            released = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            held = !released;
            isMouse = false;
            return;
        }
        pos = Input.mousePosition;
        pressed = Input.GetMouseButtonDown(0);
        released = Input.GetMouseButtonUp(0);
        held = Input.GetMouseButton(0);
#endif
    }

    void HandleInput()
    {
        ReadPointer(out var pos, out bool pressed, out bool released, out bool held, out bool isMouse);
        var gp = new Vector2(pos.x, Screen.height - pos.y);
        bool overUi = false;
        foreach (var r in uiRects) if (r.Contains(gp)) { overUi = true; break; }
        bool onScreen = pos.x >= 0 && pos.y >= 0 && pos.x <= Screen.width && pos.y <= Screen.height;

        if (phase != Phase.Play) { aiming = false; ShowDots(false); return; }

        if (pressed && !overUi && onScreen) aiming = true;
        bool hover = isMouse && !overUi && onScreen && ballsLeft > 0;
        if ((aiming || hover) && TryAimPoint(pos, out var p)) AimAt(p);
        if (aiming && (released || !held))
        {
            aiming = false;
            Fire();
        }
        ShowDots((aiming || hover) && ballsLeft > 0);
    }

    void ShowDots(bool on)
    {
        var o = PivotPos;
        for (int i = 0; i < dots.Length; i++)
        {
            if (!on) { dots[i].gameObject.SetActive(false); continue; }
            float t = (i + 1) / (float)dots.Length * aimT * 1.1f;
            dots[i].position = o + aimVel * t + 0.5f * Physics.gravity * t * t;
            dots[i].gameObject.SetActive(true);
        }
    }

    void Fire()
    {
        if (ballsLeft <= 0 || Time.time - lastShot < 0.5f || !hasAim) return;
        ballsLeft--;
        lastShot = Time.time;

        // Gülleyi namlu ağzında, öngörülen yörüngenin tam üstünde başlat.
        float dt = 1.5f / aimVel.magnitude;
        var start = PivotPos + aimVel * dt + 0.5f * Physics.gravity * dt * dt;
        var ball = Proc.Prim(PrimitiveType.Sphere, Root, Vector3.zero, Vector3.one * 0.56f, iron, true);
        ball.name = "Gulle";
        ball.layer = 2;
        ball.transform.position = start;
        var rb = ball.AddComponent<Rigidbody>();
        rb.mass = 6f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        Proc.SetVel(rb, aimVel + Physics.gravity * dt);
        ball.AddComponent<Cannonball>();

        recoil = 1f;
        flashT = 1f;
        shake = Mathf.Max(shake, 0.25f);
        PlaySfx(Sfx.Boom, 0.9f, Random.Range(0.9f, 1.05f));

        var muzzle = barrel.TransformPoint(0, 0, 1.6f);
        for (int i = 0; i < 6; i++)
        {
            var s = Proc.Prim(PrimitiveType.Sphere, Root, Vector3.zero, Vector3.one * Random.Range(0.3f, 0.55f), smokeMat, false);
            s.transform.position = muzzle + Random.insideUnitSphere * 0.2f;
            s.layer = 2;
            s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var f = s.AddComponent<AutoFade>();
            f.manual = true;
            f.vel = aimVel.normalized * Random.Range(1f, 3f) + Random.insideUnitSphere;
            f.gravityScale = -0.05f;
            f.life = Random.Range(0.5f, 0.9f);
        }
    }

    void AnimateCannon()
    {
        float dt = Time.deltaTime;
        recoil = Mathf.MoveTowards(recoil, 0f, dt * 3f);
        barrel.localPosition = Vector3.back * 0.35f * recoil * recoil;
        flashT = Mathf.MoveTowards(flashT, 0f, dt * 6f);
        flash.intensity = flashT * 8f;
        shake = Mathf.MoveTowards(shake, 0f, dt * 1.5f);
    }

    void FitCamera()
    {
        float a = (float)Screen.width / Mathf.Max(1, Screen.height);
        Vector3 pos;
        if (a < 1f) { cam.fieldOfView = 66f; pos = new Vector3(0, 5.6f, -20.5f); }
        else { cam.fieldOfView = 46f; pos = new Vector3(0, 4.3f, -17.5f); }
        var off = shake > 0f ? Random.insideUnitSphere * shake * 0.25f : Vector3.zero;
        cam.transform.position = pos + off;
        cam.transform.LookAt(new Vector3(0, 1.9f, 3f) + off * 0.5f);
    }

    // ================================================================ kayıt

    void LoadProgress()
    {
        int n = LevelCount;
        unlocked = Mathf.Clamp(PlayerPrefs.GetInt("cp_unlocked", 1), 1, n);
        stars = new int[n];
        for (int i = 0; i < n; i++) stars[i] = PlayerPrefs.GetInt("cp_star_" + i, 0);
        muted = PlayerPrefs.GetInt("cp_muted", 0) == 1;
        AudioListener.volume = muted ? 0f : 1f;
    }

    void SaveWin()
    {
        earned = ballsLeft >= 2 ? 3 : ballsLeft == 1 ? 2 : 1;
        if (earned > stars[level])
        {
            stars[level] = earned;
            PlayerPrefs.SetInt("cp_star_" + level, earned);
        }
        if (level + 2 > unlocked && level + 1 < LevelCount)
        {
            unlocked = level + 2;
            PlayerPrefs.SetInt("cp_unlocked", unlocked);
        }
        PlayerPrefs.Save();
    }

    void ToggleMute()
    {
        muted = !muted;
        AudioListener.volume = muted ? 0f : 1f;
        PlayerPrefs.SetInt("cp_muted", muted ? 1 : 0);
    }

    // ================================================================ arayüz (IMGUI — ek paket gerekmez)

    float S, stylesFor = -1f;
    Texture2D white, starTex, dotTex;
    GUIStyle sTitle, sH, sSub, sBody, sBtn, sBtnSmall, sLv, sHint;

    GUIStyle Btn(Color bg, Color hover, Color text, int size)
    {
        var st = new GUIStyle(GUI.skin.button)
        {
            fontSize = size, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(0, 0, 0, 0), padding = new RectOffset(4, 4, 4, 4)
        };
        st.normal.background = Proc.Solid(bg);
        st.hover.background = Proc.Solid(hover);
        st.active.background = Proc.Solid(hover * 0.9f);
        st.focused.background = st.normal.background;
        st.normal.textColor = st.hover.textColor = st.active.textColor = st.focused.textColor = text;
        return st;
    }

    GUIStyle Label(int size, Color c, TextAnchor anchor, bool bold = true)
    {
        var st = new GUIStyle(GUI.skin.label)
        {
            fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            alignment = anchor, wordWrap = true, clipping = TextClipping.Overflow
        };
        st.normal.textColor = c;
        return st;
    }

    void EnsureStyles()
    {
        if (white == null)
        {
            white = Proc.Solid(Color.white);
            starTex = Proc.Star(96);
            dotTex = Proc.Circle(48);
        }
        if (Mathf.Approximately(stylesFor, S)) return;
        stylesFor = S;
        int F(float px) => Mathf.Max(8, Mathf.RoundToInt(px * S));
        sTitle = Label(F(50), Chili, TextAnchor.MiddleCenter);
        sH = Label(F(26), Paper, TextAnchor.UpperLeft);
        sSub = Label(F(16), Paper, TextAnchor.UpperLeft, false);
        sBody = Label(F(19), Ink, TextAnchor.MiddleCenter, false);
        sHint = Label(F(18), Paper, TextAnchor.MiddleCenter);
        sBtn = Btn(Chili, Hex(0xf05a4f), Paper, F(26));
        sBtnSmall = Btn(Ink, Hex(0x35263f), Paper, F(16));
        sLv = Btn(Brass, Hex(0xf7c766), Ink, F(26));
        sLv.alignment = TextAnchor.UpperCenter;
        sLv.padding = new RectOffset(0, 0, F(10), 0);
    }

    void Fill(Rect r, Color c)
    {
        var o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = o;
    }

    void Icon(Rect r, Texture2D tex, Color c)
    {
        var o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
        GUI.color = o;
    }

    void Sticker(Rect r, Color face, Color edge)
    {
        Fill(new Rect(r.x, r.y + 6 * S, r.width, r.height), edge);
        Fill(r, face);
    }

    bool Button(Rect r, string label, GUIStyle st, Color edge)
    {
        uiRects.Add(r);
        Fill(new Rect(r.x, r.y + 5 * S, r.width, r.height), edge);
        bool hit = GUI.Button(r, label, st);
        if (hit) PlaySfx(Sfx.Click, 0.5f, 1f);
        return hit;
    }

    void Stars(Rect area, int count, Color on, Color off)
    {
        float sz = Mathf.Min(area.height, area.width / 3.3f);
        float gap = sz * 0.15f;
        float x = area.center.x - (3 * sz + 2 * gap) / 2f;
        for (int i = 0; i < 3; i++)
            Icon(new Rect(x + i * (sz + gap), area.y + (area.height - sz) / 2f, sz, sz), starTex, i < count ? on : off);
    }

    Rect Card(float height)
    {
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(Ink.r, Ink.g, Ink.b, 0.45f));
        float w = Mathf.Min(560 * S, Screen.width - 32 * S);
        var r = new Rect((Screen.width - w) / 2f, (Screen.height - height) / 2f, w, height);
        uiRects.Add(r);
        Sticker(r, Paper, Chili);
        return r;
    }

    void OnGUI()
    {
        S = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 720f, 0.55f, 2.2f);
        EnsureStyles();
        if (Event.current.type == EventType.Layout) uiRects.Clear();

        var safe = Screen.safeArea;
        float top = Screen.height - safe.yMax + 16 * S;
        float left = safe.xMin + 16 * S;
        float right = safe.xMax - 16 * S;

        if (phase != Phase.Menu) DrawHud(top, left, right);
        if (phase == Phase.Menu) DrawMenu();
        else if (phase == Phase.Won) DrawWon();
        else if (phase == Phase.Lost) DrawLost();
    }

    void DrawHud(float y, float x0, float x1)
    {
        int lvBalls = LevelBalls(level);
        var a = new Rect(x0, y, 300 * S, 66 * S);
        Sticker(a, new Color(Ink.r, Ink.g, Ink.b, 0.9f), Chili);
        GUI.Label(new Rect(a.x + 14 * S, a.y + 6 * S, a.width - 20 * S, 32 * S), "BÖLÜM " + (level + 1), sH);
        GUI.Label(new Rect(a.x + 14 * S, a.y + 38 * S, a.width - 20 * S, 24 * S), LevelName(level), sSub);

        float w = Mathf.Max(lvBalls * 30 * S + 28 * S, 210 * S);
        var b = new Rect(x1 - w, y, w, 66 * S);
        Sticker(b, new Color(Ink.r, Ink.g, Ink.b, 0.9f), Brass);
        for (int i = 0; i < lvBalls; i++)
            Icon(new Rect(b.x + 14 * S + i * 30 * S, b.y + 10 * S, 22 * S, 22 * S), dotTex,
                i < ballsLeft ? Brass : new Color(Paper.r, Paper.g, Paper.b, 0.22f));
        GUI.Label(new Rect(b.x + 14 * S, b.y + 38 * S, w - 20 * S, 24 * S), "Kalan hedef: " + Remaining(), sSub);

        float by = a.yMax + 18 * S, bw = 104 * S, bh = 40 * S, gap = 10 * S;
        if (Button(new Rect(x0, by, bw, bh), "Yeniden", sBtnSmall, Chili)) StartLevel(level);
        if (Button(new Rect(x0 + bw + gap, by, bw, bh), "Menü", sBtnSmall, Chili)) ShowMenu();
        if (Button(new Rect(x0 + 2 * (bw + gap), by, bw, bh), muted ? "Ses kapalı" : "Ses açık", sBtnSmall, Chili)) ToggleMute();

        if (phase == Phase.Play && level == 0 && ballsLeft == lvBalls)
        {
            float hw = Mathf.Min(560 * S, Screen.width - 32 * S);
            var h = new Rect((Screen.width - hw) / 2f, Screen.height - Screen.safeArea.yMin - 90 * S, hw, 52 * S);
            Fill(h, new Color(Ink.r, Ink.g, Ink.b, 0.8f));
            GUI.Label(h, "Basılı tut, sürükleyerek nişan al, bırakınca ateşle", sHint);
        }
    }

    void DrawMenu()
    {
        int n = LevelCount, cols = n > 12 ? 6 : 4, rows = Mathf.CeilToInt(n / (float)cols);
        float w = Mathf.Min(560 * S, Screen.width - 32 * S);
        float inner = w - 48 * S, gap = 10 * S;
        float cell = (inner - gap * (cols - 1)) / cols;
        float h = 24 * S + 64 * S + 56 * S + 20 * S + rows * cell + (rows - 1) * gap + 28 * S + 64 * S + 30 * S;
        var c = Card(h);
        float x = c.x + 24 * S, y = c.y + 24 * S;

        GUI.Label(new Rect(x, y, inner, 64 * S), "ÇÖMLEK PANAYIRI", sTitle);
        y += 64 * S;
        GUI.Label(new Rect(x, y, inner, 56 * S), "Gülleyi at, çömlekleri kır, taşları masadan düşür.", sBody);
        y += 56 * S + 20 * S;

        for (int i = 0; i < n; i++)
        {
            var r = new Rect(x + (i % cols) * (cell + gap), y + (i / cols) * (cell + gap), cell, cell);
            if (i < unlocked)
            {
                if (Button(r, (i + 1).ToString(), sLv, BrassDark)) StartLevel(i);
                Stars(new Rect(r.x + cell * 0.12f, r.yMax - cell * 0.38f, cell * 0.76f, cell * 0.26f), stars[i],
                    Ink, new Color(Ink.r, Ink.g, Ink.b, 0.2f));
            }
            else
            {
                Fill(r, Hex(0xe6dccb));
                var st = new GUIStyle(sLv);
                st.normal.textColor = Hex(0xa99c8a);
                st.normal.background = null;
                GUI.Label(r, (i + 1).ToString(), st);
            }
        }
        y += rows * cell + (rows - 1) * gap + 28 * S;

        var pr = new Rect(c.center.x - 130 * S, y, 260 * S, 60 * S);
        if (Button(pr, unlocked > 1 ? "DEVAM ET" : "OYNA", sBtn, ChiliDark)) StartLevel(unlocked - 1);
    }

    void DrawWon()
    {
        bool last = level == LevelCount - 1;
        var c = Card(24 * S + 64 * S + 90 * S + 50 * S + 20 * S + 60 * S + 16 * S + 44 * S + 30 * S);
        float x = c.x + 24 * S, w = c.width - 48 * S, y = c.y + 24 * S;
        GUI.Label(new Rect(x, y, w, 64 * S), last ? "PANAYIR BİTTİ!" : "BÖLÜM TAMAM!", sTitle);
        y += 64 * S;
        Stars(new Rect(x, y, w, 90 * S), earned, Brass, new Color(Ink.r, Ink.g, Ink.b, 0.15f));
        y += 90 * S;
        GUI.Label(new Rect(x, y, w, 50 * S),
            last ? "Bütün bölümleri bitirdin. Yıldızları tamamlamak için tekrar oyna."
                 : "Kalan gülle: " + ballsLeft + ". Her artan gülle bir yıldız daha.", sBody);
        y += 50 * S + 20 * S;
        var main = new Rect(c.center.x - 140 * S, y, 280 * S, 60 * S);
        if (last) { if (Button(main, "MENÜYE DÖN", sBtn, ChiliDark)) ShowMenu(); }
        else if (Button(main, "SONRAKİ BÖLÜM", sBtn, ChiliDark)) StartLevel(level + 1);
        y += 60 * S + 16 * S;
        if (Button(new Rect(c.center.x - 90 * S, y, 180 * S, 44 * S), "Tekrar oyna", sBtnSmall, Chili)) StartLevel(level);
    }

    void DrawLost()
    {
        var c = Card(24 * S + 64 * S + 50 * S + 20 * S + 60 * S + 16 * S + 44 * S + 30 * S);
        float x = c.x + 24 * S, w = c.width - 48 * S, y = c.y + 24 * S;
        GUI.Label(new Rect(x, y, w, 64 * S), "GÜLLE BİTTİ", sTitle);
        y += 64 * S;
        GUI.Label(new Rect(x, y, w, 50 * S), "Kalan hedef: " + Remaining() + ". Zincirleme kırılma için alttaki çömlekleri hedefle.", sBody);
        y += 50 * S + 20 * S;
        if (Button(new Rect(c.center.x - 140 * S, y, 280 * S, 60 * S), "TEKRAR DENE", sBtn, ChiliDark)) StartLevel(level);
        y += 60 * S + 16 * S;
        if (Button(new Rect(c.center.x - 90 * S, y, 180 * S, 44 * S), "Menü", sBtnSmall, Chili)) ShowMenu();
    }
}
