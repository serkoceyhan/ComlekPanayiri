using UnityEngine;

// Kırık parça, konfeti ve duman: bir süre yaşar, küçülerek kaybolur.
public class AutoFade : MonoBehaviour
{
    public float life = 2.5f;
    public bool manual;          // fizik yerine basit elle hareket
    public Vector3 vel;
    public Vector3 spin;
    public float gravityScale = 0.35f;
    float t;
    Vector3 baseScale;

    void Awake() { baseScale = transform.localScale; }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (manual)
        {
            vel += Physics.gravity * gravityScale * dt;
            vel *= 1f - Mathf.Min(1f, 1.5f * dt);
            transform.position += vel * dt;
            transform.Rotate(spin * dt);
        }
        float k = Mathf.Clamp01((life - t) / 0.4f);
        transform.localScale = baseScale * k;
        if (t >= life) Destroy(gameObject);
    }
}
