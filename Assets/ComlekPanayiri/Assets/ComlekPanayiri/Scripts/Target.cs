using UnityEngine;

// Kırılabilir çömlek veya masadan düşürülmesi gereken taş.
public class Target : MonoBehaviour
{
    public bool isJar;
    public Color color;
    public bool done;
    SmashGame game;

    public void Init(SmashGame g, bool jar, Color c)
    {
        game = g;
        isJar = jar;
        color = c;
    }

    void OnCollisionEnter(Collision c)
    {
        if (done || !isJar || game == null || !game.Armed) return;
        if (c.collider.GetComponent<AutoFade>() != null) return; // kırık parçalar zincirleme kırmasın
        float impact = c.relativeVelocity.magnitude;
        bool byBall = c.collider.GetComponent<Cannonball>() != null;
        if (impact > (byBall ? 1.2f : SmashGame.JarBreakSpeed)) Break();
    }

    void FixedUpdate()
    {
        if (done || game == null) return;
        float y = transform.position.y;
        if (isJar && y < 0.6f) Break();               // yere düşen çömlek kırılır
        else if (!isJar && y < 0.5f) { done = true; game.OnStoneDown(this); }
    }

    public void Break()
    {
        if (done) return;
        done = true;
        game.OnJarBroken(this);
        Destroy(gameObject);
    }
}
