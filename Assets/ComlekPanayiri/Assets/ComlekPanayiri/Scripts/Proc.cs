using System.Collections.Generic;
using UnityEngine;

// Prosedürel yardımcılar: malzemeler, dokular, mesh'ler ve ilkel objeler.
// Hiçbir hazır asset gerektirmez; URP ve Built-in pipeline'da çalışır.
public static class Proc
{
    static Material baseMat;

    // Aktif render pipeline'ın varsayılan malzemesini temel alır (build'de de her zaman bulunur).
    static Material Base()
    {
        if (baseMat == null)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = g.GetComponent<Renderer>().sharedMaterial;
            g.SetActive(false);
            Kill(g);
        }
        return baseMat;
    }

    public static Material Mat(Color c, float smooth = 0.25f, float metal = 0f, float emission = 0f,
                               Texture tex = null, Vector2? tiling = null)
    {
        var m = new Material(Base());
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (tex != null)
        {
            var t = tiling ?? Vector2.one;
            m.mainTexture = tex;
            m.mainTextureScale = t;
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", t); }
            if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", tex); m.SetTextureScale("_MainTex", t); }
        }
        else if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emission);
        }
        return m;
    }

    public static Texture2D Stripes(Color a, Color b, int size = 64)
    {
        var t = new Texture2D(size, 4, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < 4; y++)
                t.SetPixel(x, y, x < size / 2 ? a : b);
        t.Apply();
        return t;
    }

    public static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    public static Texture2D Circle(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
        t.Apply();
        return t;
    }

    public static Texture2D Star(int size)
    {
        var pts = new Vector2[10];
        for (int k = 0; k < 10; k++)
        {
            float a = Mathf.PI / 2f + k * Mathf.PI / 5f;
            float r = (k % 2 == 0) ? 0.48f : 0.19f;
            pts[k] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.5f + Mathf.Sin(a) * r);
        }
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < 3; sy++)
                    for (int sx = 0; sx < 3; sx++)
                        if (Inside(new Vector2((x + (sx + 0.5f) / 3f) / size, (y + (sy + 0.5f) / 3f) / size), pts)) hit++;
                t.SetPixel(x, y, new Color(1, 1, 1, hit / 9f));
            }
        t.Apply();
        return t;
    }

    static bool Inside(Vector2 p, Vector2[] poly)
    {
        bool c = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                c = !c;
        }
        return c;
    }

    // Bir profili Y ekseni etrafında döndürerek mesh üretir (çömlek, kubbe, çadır duvarı).
    public static Mesh Lathe(Vector2[] prof, int seg, bool inward = false)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        int rings = prof.Length;
        for (int s = 0; s <= seg; s++)
        {
            float a = s / (float)seg * Mathf.PI * 2f;
            float c = Mathf.Cos(a), sn = Mathf.Sin(a);
            for (int r = 0; r < rings; r++)
            {
                var p = prof[r];
                var tan = prof[Mathf.Min(r + 1, rings - 1)] - prof[Mathf.Max(r - 1, 0)];
                var n2 = new Vector2(tan.y, -tan.x).normalized;
                if (inward) n2 = -n2;
                verts.Add(new Vector3(p.x * c, p.y, p.x * sn));
                norms.Add(new Vector3(n2.x * c, n2.y, n2.x * sn).normalized);
                uvs.Add(new Vector2(s / (float)seg, r / (float)(rings - 1)));
            }
        }
        for (int s = 0; s < seg; s++)
            for (int r = 0; r < rings - 1; r++)
            {
                int a = s * rings + r, b = (s + 1) * rings + r;
                if (!inward) { tris.Add(a); tris.Add(a + 1); tris.Add(b + 1); tris.Add(a); tris.Add(b + 1); tris.Add(b); }
                else { tris.Add(a); tris.Add(b + 1); tris.Add(a + 1); tris.Add(a); tris.Add(b); tris.Add(b + 1); }
            }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetNormals(norms);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    static readonly Dictionary<PrimitiveType, Mesh> meshCache = new Dictionary<PrimitiveType, Mesh>();

    // Unity'nin hazır küp/küre/silindir mesh'ini bir kez alıp saklar.
    static Mesh PrimMesh(PrimitiveType type)
    {
        if (!meshCache.TryGetValue(type, out var m) || m == null)
        {
            var g = GameObject.CreatePrimitive(type);
            m = g.GetComponent<MeshFilter>().sharedMesh;
            g.SetActive(false);
            Kill(g);
            meshCache[type] = m;
        }
        return m;
    }

    // Oyun başında çağrılır; böylece çarpışma anında hiç geçici obje yaratılmaz.
    public static void Warmup()
    {
        foreach (PrimitiveType t in new[] { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder, PrimitiveType.Plane })
            PrimMesh(t);
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale,
                                  Material mat, bool collider)
    {
        GameObject g;
        if (collider)
        {
            g = GameObject.CreatePrimitive(type);
        }
        else
        {
            // Collider'sız obje: önce collider ekleyip sonra silmek yerine hiç eklemiyoruz.
            // (Fizik çarpışması sırasında DestroyImmediate kullanmak Unity'de hata veriyordu.)
            g = new GameObject(type.ToString());
            g.AddComponent<MeshFilter>().sharedMesh = PrimMesh(type);
            g.AddComponent<MeshRenderer>();
        }
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    // Oyun sırasında Destroy, editörde (sahne kurarken) DestroyImmediate.
    public static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    public static void SetVel(Rigidbody rb, Vector3 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}
