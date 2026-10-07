using UnityEngine;

public class Cannonball : MonoBehaviour
{
    float age;
    bool thumped;

    void Update()
    {
        age += Time.deltaTime;
        if (age > 8f || transform.position.y < -5f) Destroy(gameObject);
    }

    void OnCollisionEnter(Collision c)
    {
        if (thumped || c.relativeVelocity.magnitude < 4f) return;
        thumped = true;
        if (SmashGame.Instance != null) SmashGame.Instance.PlaySfx(Sfx.Thud, 0.5f, Random.Range(0.8f, 1.1f));
    }
}
