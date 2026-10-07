using UnityEngine;

// Sahnedeki her bölüm objesinde (Bolumler/Bolum_N) durur.
// Bölümün adını ve kaç gülle verileceğini Inspector'dan değiştirebilirsin.
public class LevelInfo : MonoBehaviour
{
    public string levelName = "Yeni Bölüm";
    [Min(1)] public int balls = 3;
}
