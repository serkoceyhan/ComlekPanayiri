// Bölümler. Yeni bölüm eklemek için listeye bir LevelDef daha ekle.
// Koordinatlar: x sağ/sol, z derinlik (top -11'de, sahne +2..+8 arası), y yükseklik.
// Table(x, z, genişlik, derinlik, yükseklik [, kayma genliği, hız])
// Jar(x, altY, z)  Stone(x, altY, z)  Plank(x, altY, z, uzunluk)
public class LevelDef
{
    public string name;
    public int balls;
    public System.Action<SmashGame> build;
}

public static class Levels
{
    public static readonly LevelDef[] All =
    {
        new LevelDef { name = "Isınma", balls = 3, build = g =>
        {
            g.Table(0, 3, 4f, 1.4f, 1.2f);
            g.Jar(-1.2f, 1.2f, 3); g.Jar(0, 1.2f, 3); g.Jar(1.2f, 1.2f, 3);
        }},
        new LevelDef { name = "Piramit", balls = 3, build = g =>
        {
            g.Table(0, 3, 4.2f, 1.4f, 1.2f);
            g.Jar(-0.8f, 1.2f, 3); g.Jar(0, 1.2f, 3); g.Jar(0.8f, 1.2f, 3);
            g.Plank(0, 2.0f, 3, 2.4f);
            g.Jar(-0.4f, 2.15f, 3); g.Jar(0.4f, 2.15f, 3);
            g.Plank(0, 2.95f, 3, 1.4f);
            g.Jar(0, 3.1f, 3);
        }},
        new LevelDef { name = "İki Masa", balls = 4, build = g =>
        {
            g.Table(-2.5f, 2, 2.6f, 1.3f, 1.0f);
            g.Jar(-3.2f, 1.0f, 2); g.Jar(-2.5f, 1.0f, 2); g.Jar(-1.8f, 1.0f, 2);
            g.Table(2.5f, 6, 2.6f, 1.3f, 1.8f);
            g.Jar(1.8f, 1.8f, 6); g.Jar(2.5f, 1.8f, 6); g.Stone(3.25f, 1.8f, 6);
        }},
        new LevelDef { name = "Taş Duvar", balls = 5, build = g =>
        {
            g.Table(0, 2, 5f, 1f, 1.0f);
            g.Stone(-1.5f, 1.0f, 2); g.Stone(-0.5f, 1.0f, 2); g.Stone(0.5f, 1.0f, 2); g.Stone(1.5f, 1.0f, 2);
            g.Stone(-1f, 1.7f, 2); g.Stone(0, 1.7f, 2); g.Stone(1f, 1.7f, 2);
            g.Table(0, 5.5f, 4f, 1.2f, 1.4f);
            g.Jar(-1.2f, 1.4f, 5.5f); g.Jar(-0.4f, 1.4f, 5.5f); g.Jar(0.4f, 1.4f, 5.5f); g.Jar(1.2f, 1.4f, 5.5f);
        }},
        new LevelDef { name = "İkiz Kuleler", balls = 4, build = g =>
        {
            g.Table(-2f, 3, 2.2f, 1.3f, 1.0f);
            Tower(g, -2f, 3, 1.0f);
            g.Table(2f, 6, 2.2f, 1.3f, 1.6f);
            Tower(g, 2f, 6, 1.6f);
        }},
        new LevelDef { name = "Kayan Tezgâh", balls = 5, build = g =>
        {
            g.Table(0, 4, 2.6f, 1.2f, 1.4f, 3f, 0.9f);
            g.Jar(-0.8f, 1.4f, 4); g.Jar(0, 1.4f, 4); g.Jar(0.8f, 1.4f, 4);
            g.Table(-4.5f, 7, 1.4f, 1.2f, 2.2f); g.Jar(-4.5f, 2.2f, 7);
            g.Table(4.5f, 7, 1.4f, 1.2f, 2.2f); g.Jar(4.5f, 2.2f, 7);
        }},
        new LevelDef { name = "Yüksek Raf", balls = 4, build = g =>
        {
            g.Table(0, 6, 5f, 1.2f, 2.6f);
            g.Stone(-0.8f, 2.6f, 6); g.Stone(0, 2.6f, 6); g.Stone(0.8f, 2.6f, 6);
            g.Stone(-0.4f, 3.3f, 6); g.Stone(0.4f, 3.3f, 6);
            g.Jar(0, 4.0f, 6);
            g.Jar(-2f, 2.6f, 6); g.Jar(2f, 2.6f, 6);
        }},
        new LevelDef { name = "Büyük Final", balls = 6, build = g =>
        {
            g.Table(-3f, 3, 2.6f, 1.2f, 1.0f);
            g.Jar(-3.8f, 1.0f, 3); g.Jar(-3f, 1.0f, 3); g.Jar(-2.2f, 1.0f, 3);
            g.Plank(-3f, 1.8f, 3, 2.2f);
            g.Jar(-3.4f, 1.95f, 3); g.Jar(-2.6f, 1.95f, 3);
            g.Table(3f, 3, 2.6f, 1.2f, 1.0f);
            g.Stone(2.5f, 1.0f, 3); g.Stone(3.5f, 1.0f, 3); g.Jar(3f, 1.7f, 3);
            g.Table(0, 7, 4f, 1.2f, 2.2f);
            g.Jar(-1.2f, 2.2f, 7); g.Jar(-0.4f, 2.2f, 7); g.Jar(0.4f, 2.2f, 7); g.Jar(1.2f, 2.2f, 7);
            g.Plank(0, 3.0f, 7, 3.2f);
            g.Jar(-0.6f, 3.15f, 7); g.Jar(0.6f, 3.15f, 7);
        }},
    };

    static void Tower(SmashGame g, float x, float z, float h)
    {
        g.Jar(x - 0.5f, h, z); g.Jar(x + 0.5f, h, z);
        g.Plank(x, h + 0.8f, z, 1.8f);
        g.Jar(x - 0.45f, h + 0.95f, z); g.Jar(x + 0.45f, h + 0.95f, z);
        g.Plank(x, h + 1.75f, z, 1.4f);
        g.Jar(x, h + 1.9f, z);
    }
}
