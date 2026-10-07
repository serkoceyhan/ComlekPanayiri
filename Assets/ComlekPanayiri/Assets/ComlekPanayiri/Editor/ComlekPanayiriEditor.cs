using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Unity'nin üst menüsüne "Çömlek Panayırı" ekler.
//  - Sahneyi Kur: panayırı, topu ve bütün bölümleri gerçek objeler olarak sahneye koyar.
//  - Bölüm Düzenleyici: bölümleri gösterme, kopyalama, yeni bölüm ekleme penceresi.
public static class ComlekPanayiriEditor
{
    public const string Root = "Assets/ComlekPanayiri";
    public const string GenFolder = Root + "/Uretilen";
    public const string PrefabFolder = GenFolder + "/Prefablar";
    static int counter;

    static readonly string[] PieceNames =
    {
        "Masa", "Comlek_Kirmizi", "Comlek_Yesil", "Comlek_Sari", "Comlek_Mavi", "Comlek_Pembe", "Comlek_Fistik",
        "Tas", "Tahta"
    };

    public static SmashGame FindGame()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<SmashGame>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<SmashGame>(true);
#endif
    }

    [MenuItem("Çömlek Panayırı/Sahneyi Kur", false, 1)]
    public static void BuildScene()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Çömlek Panayırı", "Önce Play'i durdur, sonra tekrar dene.", "Tamam");
            return;
        }

        var existing = FindGame();
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Çömlek Panayırı",
                    "Sahnede zaten bir panayır var. Silip Levels.cs'teki bölümlerle baştan kurayım mı?\n\n" +
                    "Sahnede bölümlere yaptığın değişiklikler silinir.", "Baştan kur", "Vazgeç"))
                return;
            RemoveExisting(existing);
        }

        if (AssetDatabase.IsValidFolder(GenFolder)) AssetDatabase.DeleteAsset(GenFolder);
        EnsureFolder(Root);
        EnsureFolder(GenFolder);
        EnsureFolder(PrefabFolder);
        counter = 0;

        var game = new GameObject("ComlekPanayiri").AddComponent<SmashGame>();
        game.BuildForEditor();

        // Elle eklemek için parça prefab'ları (Project > ComlekPanayiri > Uretilen > Prefablar)
        var tmp = new GameObject("_parcalar");
        game.SetBuildRoot(tmp);
        game.Table(0, 0, 4f, 1.4f, 1.2f);
        for (int c = 0; c < 6; c++) game.Jar(0, 0, 0, c);
        game.Stone(0, 0, 0);
        game.Plank(0, 0, 0, 2f);
        game.SetBuildRoot(null);

        // Kodla üretilen malzeme, doku ve mesh'leri asset olarak kaydet (yoksa sahne kaydedilince kaybolur).
        foreach (var go in new[]
                 {
                     game.decorRoot ? game.decorRoot.gameObject : null,
                     game.cannonRoot ? game.cannonRoot.gameObject : null,
                     game.levelsRoot ? game.levelsRoot.gameObject : null,
                     game.sceneCam ? game.sceneCam.gameObject : null,
                     tmp
                 })
            if (go != null) Persist(go);

        int n = Mathf.Min(PieceNames.Length, tmp.transform.childCount);
        var pieces = new List<GameObject>();
        for (int i = 0; i < n; i++) pieces.Add(tmp.transform.GetChild(i).gameObject);
        for (int i = 0; i < n; i++)
        {
            pieces[i].transform.SetParent(null, true);
            pieces[i].name = PieceNames[i];
            pieces[i].transform.position = Vector3.zero;
            PrefabUtility.SaveAsPrefabAsset(pieces[i], PrefabFolder + "/" + PieceNames[i] + ".prefab");
            Object.DestroyImmediate(pieces[i]);
        }
        Object.DestroyImmediate(tmp);
        AssetDatabase.SaveAssets();

        foreach (var t in new[] { game.transform, game.decorRoot, game.cannonRoot, game.levelsRoot,
                                  game.sceneCam ? game.sceneCam.transform : null })
            if (t != null) Undo.RegisterCreatedObjectUndo(t.gameObject, "Çömlek Panayırı kur");
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        if (game.levelsRoot != null && game.levelsRoot.childCount > 0)
            Selection.activeGameObject = game.levelsRoot.GetChild(0).gameObject;
        LevelEditorWindow.Open();
        Debug.Log("Çömlek Panayırı sahneye kuruldu. Sahneyi kaydetmeyi unutma (Ctrl+S).");
    }

    [MenuItem("Çömlek Panayırı/Bölüm Düzenleyici", false, 2)]
    static void OpenWindow() => LevelEditorWindow.Open();

    static void RemoveExisting(SmashGame g)
    {
        if (g.decorRoot) Object.DestroyImmediate(g.decorRoot.gameObject);
        if (g.cannonRoot) Object.DestroyImmediate(g.cannonRoot.gameObject);
        if (g.levelsRoot) Object.DestroyImmediate(g.levelsRoot.gameObject);
        if (g.sceneCam) Object.DestroyImmediate(g.sceneCam.gameObject);
        Object.DestroyImmediate(g.gameObject);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static void Persist(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials) SaveMaterial(m);
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) Save(mf.sharedMesh, "Mesh", ".asset");
        foreach (var mc in root.GetComponentsInChildren<MeshCollider>(true)) Save(mc.sharedMesh, "Mesh", ".asset");
    }

    static void SaveMaterial(Material m)
    {
        if (m == null || EditorUtility.IsPersistent(m)) return;
        foreach (var prop in m.GetTexturePropertyNames())
            Save(m.GetTexture(prop), "Doku", ".asset");
        Save(m, "Malzeme", ".mat");
    }

    static void Save(Object o, string prefix, string ext)
    {
        if (o == null || EditorUtility.IsPersistent(o)) return;
        var path = AssetDatabase.GenerateUniqueAssetPath(GenFolder + "/" + prefix + "_" + (++counter) + ext);
        AssetDatabase.CreateAsset(o, path);
    }
}

public class LevelEditorWindow : EditorWindow
{
    Vector2 scroll;

    public static void Open()
    {
        var w = GetWindow<LevelEditorWindow>("Çömlek Panayırı");
        w.minSize = new Vector2(320, 300);
    }

    void OnHierarchyChange() => Repaint();

    void OnGUI()
    {
        var game = ComlekPanayiriEditor.FindGame();
        if (game == null || game.levelsRoot == null)
        {
            EditorGUILayout.HelpBox(
                "Bu sahnede panayır kurulu değil. Aşağıdaki düğme panayırı, topu ve bütün bölümleri sahneye koyar.",
                MessageType.Info);
            if (GUILayout.Button("Sahneyi Kur", GUILayout.Height(32))) ComlekPanayiriEditor.BuildScene();
            return;
        }
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Oyun çalışıyor. Bölümleri düzenlemek için Play'i durdur.", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox(
            "Göster'e bas, o bölüm Scene penceresinde görünsün. Objeleri taşı, Ctrl+D ile kopyala, Delete ile sil.\n" +
            "Yeni parça: Project > ComlekPanayiri > Uretilen > Prefablar klasöründen sürükleyip Hierarchy'de " +
            "bölümün içine bırak. Bölüm adı ve gülle sayısı: bölüm objesinin Inspector'ındaki Level Info.\n" +
            "Play'e basınca oyun bu sahnedeki bölümleri sırayla oynatır. Kaydetmeyi unutma (Ctrl+S).",
            MessageType.Info);

        Transform shown = null;
        scroll = EditorGUILayout.BeginScrollView(scroll);
        int index = 0;
        foreach (Transform child in game.levelsRoot)
        {
            var info = child.GetComponent<LevelInfo>();
            if (info == null) continue;
            index++;
            bool active = child.gameObject.activeSelf;
            if (active) shown = child;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(index + ". " + info.levelName + "  (" + info.balls + " gülle)",
                active ? EditorStyles.boldLabel : EditorStyles.label);
            if (GUILayout.Button(active ? "Görünüyor" : "Göster", GUILayout.Width(90))) Show(child);
            if (GUILayout.Button("Kopyala", GUILayout.Width(70))) Duplicate(child);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Yeni bölüm ekle", GUILayout.Height(26))) NewLevel(game.levelsRoot);
        using (new EditorGUI.DisabledScope(shown == null || Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Seçili objeleri görünen bölüme taşı")) MoveSelection(shown);
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("Sahneyi baştan kur")) ComlekPanayiriEditor.BuildScene();
    }

    static void Show(Transform t)
    {
        foreach (Transform c in t.parent)
        {
            Undo.RecordObject(c.gameObject, "Bölüm göster");
            c.gameObject.SetActive(c == t);
        }
        Selection.activeGameObject = t.gameObject;
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
    }

    static void Duplicate(Transform t)
    {
        var copy = Object.Instantiate(t.gameObject, t.parent);
        copy.name = "Bolum_" + t.parent.childCount;
        var info = copy.GetComponent<LevelInfo>();
        if (info != null) info.levelName += " (kopya)";
        copy.transform.SetAsLastSibling();
        Undo.RegisterCreatedObjectUndo(copy, "Bölüm kopyala");
        Show(copy.transform);
    }

    static void NewLevel(Transform levelsRoot)
    {
        var go = new GameObject("Bolum_" + (levelsRoot.childCount + 1));
        go.transform.SetParent(levelsRoot, false);
        var info = go.AddComponent<LevelInfo>();
        info.levelName = "Yeni Bölüm";
        info.balls = 3;
        var table = AssetDatabase.LoadAssetAtPath<GameObject>(ComlekPanayiriEditor.PrefabFolder + "/Masa.prefab");
        var jar = AssetDatabase.LoadAssetAtPath<GameObject>(ComlekPanayiriEditor.PrefabFolder + "/Comlek_Kirmizi.prefab");
        if (table != null)
        {
            var t = (GameObject)PrefabUtility.InstantiatePrefab(table, go.transform);
            t.transform.position = new Vector3(0, 1.2f, 3f);
        }
        if (jar != null)
        {
            var j = (GameObject)PrefabUtility.InstantiatePrefab(jar, go.transform);
            j.transform.position = new Vector3(0, 1.2f + 0.405f, 3f);
        }
        Undo.RegisterCreatedObjectUndo(go, "Yeni bölüm");
        Show(go.transform);
    }

    static void MoveSelection(Transform level)
    {
        foreach (var g in Selection.gameObjects)
        {
            if (g.transform == level || level.IsChildOf(g.transform)) continue;
            Undo.SetTransformParent(g.transform, level, "Bölüme taşı");
        }
    }
}
