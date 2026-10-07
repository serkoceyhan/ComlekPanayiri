# Çömlek Panayırı

A physics-based cannon game made in Unity: fire cannonballs at a fairground stall, shatter clay jars, and knock heavy stones off the tables. Every unused cannonball earns an extra star.

![Gameplay](docs/oynanis.gif)

**▶ Play in the browser:** [itch.io](https://KULLANICI_ADIN.itch.io/comlek-panayiri)

## Features

- **8 levels** with pyramids, twin towers, a stone wall, a sliding counter and a high shelf
- **Ballistic aiming:** the cannon solves the launch velocity to hit the point under your cursor, and a dotted line previews the arc
- **Chain reactions:** knock out a bottom jar and the planks and jars above fall and break on impact
- **No imported assets:** jar meshes, striped textures, materials and every sound effect are generated in code
- **Editor tooling:** a *Çömlek Panayırı* menu builds the whole scene in the editor, and a level editor window lets you show, duplicate and create levels with drag-and-drop prefabs
- Works with URP and Built-in render pipelines, the new and old Input Systems, desktop and mobile (portrait and landscape)

## Controls

| Platform | Aim | Fire |
|---|---|---|
| Mouse | Move the cursor | Click |
| Touch | Hold and drag | Release |

## Running the project

1. Open the project in **Unity 6** (tested on 6000.3). 2022.3 LTS should also work.
2. Open any scene and press **Play**. The game builds itself at runtime.
3. Optional: **Çömlek Panayırı → Sahneyi Kur** puts everything into the scene so you can edit levels visually.

## Code overview

| File | Purpose |
|---|---|
| `SmashGame.cs` | Game loop, aiming and firing, camera, UI (IMGUI), save data |
| `Levels.cs` | Level layouts written in code |
| `Target.cs` | Jar breaking (impact speed threshold) and stone-off-table detection |
| `Proc.cs` | Procedural meshes (lathe), textures, materials |
| `Sfx.cs` | Synthesized audio (cannon boom, ceramic crash, thud, jingles) |
| `Editor/ComlekPanayiriEditor.cs` | Scene builder menu and level editor window |

## How it was made

I built this with **Claude** (Anthropic's AI assistant) as a pair programmer. I came up with the game idea and chose the engine. I tested every version in Unity, tracked down bugs from the console and fed them back, and asked for the editor tools so I could build levels visually. Claude wrote the C# code.

Inspired by the mobile game *Smash Fest*. This project is not affiliated with it and uses none of its assets.

---

## Türkçe

Unity ile yapılmış fizik tabanlı bir top atma oyunu: panayır tezgâhına gülle atıyorsun, çömlekleri kırıyorsun, ağır taşları masadan düşürüyorsun. Artan her gülle bir yıldız daha kazandırıyor.

- 8 bölüm, zincirleme kırılmalar, nişan alırken görünen gülle yolu
- Hiç hazır asset yok: modeller, dokular ve sesler kodla üretiliyor
- Unity menüsünden sahneyi kurup bölümleri sürükle bırakla düzenleyebiliyorsun

Projeyi Unity 6'da aç ve Play'e bas. Ayrıntılar için `BENIOKU.md` dosyasına bak.

Oyunu yapay zekâ asistanı Claude ile birlikte geliştirdim. Fikir, test, hata ayıklama ve yön benden geldi, C# kodunu Claude yazdı.

## License

MIT
