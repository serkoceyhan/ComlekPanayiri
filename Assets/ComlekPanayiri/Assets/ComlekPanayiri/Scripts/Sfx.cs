using UnityEngine;

// Ses dosyası gerektirmeyen, kodla üretilen efektler.
public static class Sfx
{
    const int SR = 44100;
    public static AudioClip Boom, Crash, Thud, Win, Lose, Click;
    static System.Random rng = new System.Random(7);
    static float N() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

    static AudioClip Make(string name, float dur, System.Func<float, float> f)
    {
        int n = (int)(SR * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
        var c = AudioClip.Create(name, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    public static void Build()
    {
        if (Boom != null) return;

        float lp = 0f, ph = 0f;
        Boom = Make("boom", 0.8f, t =>
        {
            lp += (N() - lp) * 0.08f;
            float f = 40f + 70f * Mathf.Exp(-t * 8f);
            ph += 2f * Mathf.PI * f / SR;
            return (lp * 2.2f * Mathf.Exp(-t * 6f) + Mathf.Sin(ph) * 0.8f * Mathf.Exp(-t * 5f)) * 0.9f;
        });

        float lp2 = 0f;
        Crash = Make("crash", 0.45f, t =>
        {
            float n = N();
            lp2 += (n - lp2) * 0.25f;
            float hiss = (n - lp2) * Mathf.Exp(-t * 14f);
            float ping = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-t * 22f)
                       + Mathf.Sin(2f * Mathf.PI * 2730f * t) * Mathf.Exp(-t * 30f) * 0.7f
                       + Mathf.Sin(2f * Mathf.PI * 3620f * t) * Mathf.Exp(-t * 40f) * 0.5f;
            return hiss * 0.9f + ping * 0.35f;
        });

        float lp3 = 0f, ph3 = 0f;
        Thud = Make("thud", 0.3f, t =>
        {
            lp3 += (N() - lp3) * 0.05f;
            ph3 += 2f * Mathf.PI * (55f + 85f * Mathf.Exp(-t * 18f)) / SR;
            return (Mathf.Sin(ph3) * 0.9f + lp3 * 1.5f) * Mathf.Exp(-t * 14f);
        });

        Win = Notes("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.13f);
        Lose = Notes("lose", new[] { 392f, 329.6f, 261.6f }, 0.2f);
        Click = Make("click", 0.06f, t => Mathf.Sin(2f * Mathf.PI * 900f * t) * Mathf.Exp(-t * 60f) * 0.5f);
    }

    static AudioClip Notes(string name, float[] freqs, float step)
    {
        float dur = step * freqs.Length + 0.35f;
        return Make(name, dur, t =>
        {
            float s = 0f;
            for (int i = 0; i < freqs.Length; i++)
            {
                float lt = t - i * step;
                if (lt < 0f) continue;
                float tri = Mathf.PingPong(lt * freqs[i] * 2f, 2f) - 1f;
                s += tri * Mathf.Exp(-lt * 7f) * 0.3f;
            }
            return s;
        });
    }
}
