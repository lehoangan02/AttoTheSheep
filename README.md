# Atto The Sheep

[![Game Site](https://img.shields.io/badge/Play_on-Itch.io-FA5C5C?style=for-the-badge&logo=itch.io)](https://lehoangan02.itch.io/atto-the-sheep)
[![Trailer](https://img.shields.io/badge/Watch-Trailer-FF0000?style=for-the-badge&logo=youtube)](https://www.youtube.com/watch?v=CfjfNgLRITM)
[![Walkthrough](https://img.shields.io/badge/Watch-Walkthrough-FF0000?style=for-the-badge&logo=youtube)](https://youtu.be/LihEDBNJEBg)

An ambitious, action-packed 2D top-down RPG and escort survival game built as the midterm project for the **3D Visualization and Game Development** course. 

## 👨‍💻 Development Team

- **Trần Đức An** - 23125024
- **Lê Hoàng Ân** - 23125025
- **Hoàng Tuấn Khoa** - 23125060
- **Đỗ Phan Tuấn Phát** - 23125065

---

## 🎮 About The Game

**Atto The Sheep** is a robust, fully-featured commercial-quality vertical slice. The player takes control of Atto, a genetic anomaly—a highly intelligent, mutated sheep blessed with extraordinary physical strength. When the tyrannical King’s army raids the local peaceful farm, Atto must protect his flock of defenseless lambs from waves of enemies (soldiers, giant spiders, skeleton warriors, and the Great Yokai).

The game blends the fast-paced action of traditional top-down role-playing games (RPGs) with the strategic, high-stakes tension of an escort mission. Protect the flock to maintain your damage buffs, strategically manage Atto's regenerating mana, and survive across three distinct biomes, culminating in an apocalyptic boss fight. The game also features a separate **Multiplayer Co-op Colosseum** mode where two players can survive infinite waves together over the internet!

---

## ⚙️ Technical Specifications

- **Engine:** Unity
- **Unity Version:** `6000.3.16f1`
- **Supported Platforms:** macOS, Ubuntu (Linux), Windows, Android
- **Architecture:** Domain-Driven Design (DDD) for Save Systems/Networking, Event-Driven UI.

---

## 📚 References & Resources

The development of this project utilized several advanced algorithms, third-party packages, and resources:

### Core Systems & Algorithms
- **Boids Flocking Algorithm:** Inspired by Craig Reynolds' original Boids algorithm for the autonomous herd management of the lambs.
- **Context Steering 2D:** Custom pathfinding architecture utilized for enemy swarms to avoid environmental obstacles fluidly without the massive CPU overhead of traditional A*.

### Unity Packages & Third-Party Tools
- **Unity Netcode for GameObjects (NGO) & Unity Relay:** Utilized for building the server-authoritative Multiplayer Co-op mode.
- **Unity Cloud Save:** Utilized for the Domain-Driven cross-platform save profile system.
- **ParrelSync:** Open-source tool used for testing multiplayer functionality locally within the Unity Editor.
- **Quantum Console:** Advanced in-game developer console used for rapid prototyping, balancing, and cheat commands.
- **Unity Recorder:** Used to capture the 4K uncompressed marketing trailer and gameplay walkthrough.

### Typography & Visual Identity
- **Custom Fonts:** 
  - *Bangers* (Damage popups)
  - *Lilita One* (UI and dialog bubbles)
  - *Press Start 2P* (Summary screens)
- *(Note: TextMeshPro fallback fonts were heavily utilized to properly render Vietnamese diacritics lacking in these stylized fonts).*