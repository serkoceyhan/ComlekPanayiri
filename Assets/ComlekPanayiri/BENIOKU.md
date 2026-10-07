# Çömlek Panayırı (Unity)

Smash Fest tarzı bir fizik oyunu: toptan gülle atıyorsun, çömlekleri kırıyorsun, taşları masadan düşürüyorsun. Gülle sayın sınırlı, artan her gülle sana bir yıldız daha kazandırıyor.

## Kurulum (5 dakika)

1. **Unity Hub**'ı aç ve **Unity 6 LTS** kur (2022.3 LTS de çalışır).
2. **New project** → **Universal 3D** şablonunu seç (ya da "3D (Built-In)"), projeye isim ver, oluştur.
3. Bu zipteki `Assets/ComlekPanayiri` klasörünü, projenin `Assets` klasörünün içine sürükle.
4. Unity derlemeyi bitirince (sağ altta dönen simge durunca) hangi sahne açıksa onda **Play**'e bas.

Sahneye bir şey eklemen gerekmez. Oyun kamerayı, ışığı, panayırı, topu, bölümleri, arayüzü ve sesleri kendisi kuruyor. Sahnede duran varsayılan kamera ve ışığı kendisi kapatıyor.

## Sahneyi Unity içinde görmek ve düzenlemek

Play'e basmadan da her şeyi Scene penceresinde görebilirsin:

1. Üst menüden **Çömlek Panayırı → Sahneyi Kur**'a bas.
2. Panayır, top ve 8 bölümün hepsi gerçek objeler olarak sahneye gelir. Hierarchy'de şöyle görünür:
   - `Kamera`, `Panayir` (çadır, zemin, ampuller), `Top`
   - `Bolumler` → `Bolum_1` … `Bolum_8`
3. **Çömlek Panayırı** penceresi açılır. Bir bölümün yanındaki **Göster**'e basınca o bölüm görünür, diğerleri gizlenir.
4. Masaları ve çömlekleri fareyle taşı (W tuşu), **Ctrl+D** ile kopyala, **Delete** ile sil.
5. Yeni parça eklemek için **Project → ComlekPanayiri → Uretilen → Prefablar** klasöründen Masa, Çömlek, Taş ya da Tahta'yı sürükleyip Hierarchy'de **bölümün içine** bırak. Bıraktıktan sonra Y yüksekliğine bak, parça masanın içine gömülü kalmasın. Yanlış yere bıraktıysan objeleri seç ve pencerede **Seçili objeleri görünen bölüme taşı**'ya bas.
6. Bölümün adı ve gülle sayısı, bölüm objesinin Inspector'ındaki **Level Info** bölümünde. **Kopyala** ve **Yeni bölüm ekle** düğmeleriyle bölüm sayısını istediğin kadar artırabilirsin.
7. **Ctrl+S** ile sahneyi kaydet ve Play'e bas. Oyun artık `Bolumler` altındaki bölümleri Hierarchy'deki sırayla oynatır.

Sahneyi kurmadan Play'e basarsan oyun eskisi gibi her şeyi `Levels.cs`'ten kodla kurar.

## Nasıl oynanır

- **Fare:** imleci gezdirince nişan çizgisi görünür, tıklayınca gülle atılır.
- **Telefon:** parmağını basılı tutup sürükleyerek nişan al, bırakınca ateşle.
- Bütün çömlekler kırılıp bütün taşlar yere düşünce bölüm biter.
- Alttaki çömleği kırarsan üsttekiler düşüp zincirleme kırılır.

## Telefona çıkarmak

**File → Build Profiles** (2022'de **Build Settings**) → **Android** ya da **iOS** → **Switch Platform** → **Build**. Ekran yönü olarak dikey ve yatay ikisi de destekleniyor, kamera kendini ayarlıyor.

## Neyi nereden değiştirirsin

| Dosya | İçinde ne var |
|---|---|
| `Editor/ComlekPanayiriEditor.cs` | "Çömlek Panayırı" menüsü ve bölüm düzenleyici penceresi. |
| `Levels.cs` | 8 bölüm. "Sahneyi Kur" bunları sahneye koyar. Yeni bölüm eklemek için listeye bir blok daha yaz. `Table`, `Jar`, `Stone`, `Plank` ile masaları ve hedefleri diziyorsun. |
| `SmashGame.cs` | Oyunun kendisi: top, nişan, kamera, menü, yıldızlar, kayıt. En üstte renkler ve `JarBreakSpeed` (çömleğin ne kadar kolay kırılacağı) var. |
| `Target.cs` | Çömlek kırılma ve taş düşme kuralları. |
| `Sfx.cs` | Kodla üretilen sesler (patlama, cam kırılması, tok ses, kazanma). |
| `Proc.cs` | Çömlek şekli, çizgili doku gibi prosedürel modeller. |

İlerleme (açılan bölümler ve yıldızlar) `PlayerPrefs` ile cihazda saklanıyor. Sıfırlamak için Unity'de **Edit → Clear All PlayerPrefs**.

## Sonraki adımlar için fikirler

- Gerçek 3D çömlek modelleri ve kırılma parçaları (Asset Store'da ücretsiz olanlar var).
- TextMeshPro ile daha şık menüler.
- Patlayan varil, cam şişe, hareketli hedef gibi yeni hedef türleri.
- 100 bölüme çıkmak için `Levels.cs` içine rastgele bölüm üretici.
