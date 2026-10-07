using UnityEngine;

// Sağa sola kayan tezgah (kinematik rigidbody).
public class MovingPlatform : MonoBehaviour
{
    public float amp = 3f;
    public float speed = 0.9f;
    Vector3 origin;
    Rigidbody rb;
    float t;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        origin = transform.position;
    }

    void FixedUpdate()
    {
        t += Time.fixedDeltaTime;
        rb.MovePosition(origin + Vector3.right * Mathf.Sin(t * speed) * amp);
    }
}
