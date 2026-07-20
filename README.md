# 🗂️ HDA Archiver

**A modern archive manager for PS2 Harvest Moon: Save The Homeland (.HDA) files.**

Built as a standalone Windows application, HDA Archiver lets you open, create, modify, and repack `.HDA` archives from the game — with full support for the game's Smart LZO compression, drag-and-drop file management, and automatic file type detection.

Created by **DarthKrayt333** as a companion tool to **[HMSTHModdingTool](https://github.com/DarthKrayt333/HMSTHModdingTool)**.

---

<img width="883" height="590" alt="image_10" src="https://github.com/user-attachments/assets/18d7a55a-ccb2-4654-98cc-dd8e5cd64f43" />

---

## ✨ Features

- 📦 **Open, create, and save** `.HDA` archives
- 🔍 **Automatic file extension detection** for `.rdtb` `.gdtb` `.srdb` `.elf` `.HDA` `.BD` `.HD` `.SQ` and more
- 🗜️ **Smart LZO compression** — matches original HMSTH game engine
- 🖱️ **Full drag & drop support** — drag files IN and OUT of archives
- ✏️ **Right-click menu** — Replace, Rename, Extract, Remove
- ⚡ **Quick extract** to auto-named folder next to the `.HDA`
- 🌙 **Dark modern UI** with clean, readable file listing
- 🎯 **Nested HDA support** — for archives like `HAYATO.HDA` containing `HAYATO_02.HDA`
- 🎨 **Any file format accepted** — game files, images, audio, whatever you drop
- 🖼️ **File association** — register as default `.HDA` handler in Windows Explorer
- 🚀 **Portable single .exe** — no installer, no dependencies (beyond .NET Framework)

---

## 🖥️ System Requirements

| Requirement | Version |
|---|---|
| **OS** | Windows 10 / 11 |
| **Architecture** | 32-bit and 64-bit both supported (AnyCPU build) |
| **Runtime** | .NET Framework 4.x (Windows 10/11 includes it; compatible with apps targeting .NET Framework 4.0) |

---

## 📥 Installation

### Option 1 — Download Pre-built Release
1. Go to the Release page
2. Download the latest `HDAArchiver.zip`
3. Extract anywhere you like
4. Run `HDAArchiver.exe` — that's it, no installer needed

### Option 2 — Build from Source
1. Clone this repository
2. Open `HDAArchiver.sln` in Visual Studio 2019+
3. Set configuration to **Release** and platform to **Any CPU**
4. Build → Rebuild Solution
5. Run the `.exe` from `bin\Release\`

---

## 🚀 Quick Start

### Opening an Archive
- **Double-click** an `.HDA` file (if app is registered as handler)
- **File → Open HDA...** from the menu
- **Drag** an `.HDA` onto the app window (only when no archive is open)

### Creating a New Archive
1. Launch the app — an empty untitled archive is ready immediately
2. Drag files into the window (any format works!)
3. **File → Save** — choose where to save the `.HDA`

### Managing Files
| Action | How |
|---|---|
| **Add files** | Drag & drop into the app, or Archive → Add Files |
| **Replace file** | Right-click file → Replace with File... |
| **Rename file** | Right-click file → Rename... |
| **Extract single/multiple** | Right-click → Extract Selected... |
| **Extract all** | Archive → Extract All..., or **Ctrl+E** |
| **Quick extract** | Archive → Extract to HDA-Named Folder, or **Ctrl+D** |
| **Remove file** | Select and press **Delete** |
| **Drag files out** | Select and drag to Explorer or desktop |

---

## 🔧 How .HDA Files Work

`.HDA` (Harvest Data Archive) is the container format used by PS2 Harvest Moon: Save The Homeland. Each `.HDA` contains:

- **16-byte header** — with base offset (usually `0x10`)
- **Offset table** — pointing to each contained file
- **File entries** — each with a 16-byte sub-header:
  - `compressedFlag` (4 bytes) — `1` = LZO compressed, `0` = raw
  - `decompressedSize` (4 bytes) — original file size
  - `storedSize` (4 bytes) — size in archive
  - `padding` (4 bytes)
- **Empty slots** — supported (zeros in offset table)

### Detected File Types Inside .HDA

| Extension | Description |
|---|---|
| `.rdtb` | 3D model archive (Ryu Data Table Binary) |
| `.gdtb` | BMP texture archive (Graphics Data Table Binary) |
| `.srdb` | Container of small .rdtb blobs |
| `.HDA` | Nested archive |
| `.BD` | PS2 ADPCM audio body |
| `.HD` | Audio header |
| `.SQ` | Audio Midi sequence |
| `.elf` | PS2 executable |
| `.bin` | Fallback for unknown formats |

---

## ⚙️ Settings

Access via **Tools → Settings...**

- **Compress files when packing** — Smart LZO compression (default: ON)
- **Confirm before removing entries** — safety prompt (default: ON)
- **Show uncompressed/compressed size columns** — display toggles

---

## 🎮 About Harvest Moon: Save The Homeland

Harvest Moon: Save The Homeland is a 2001 PS2 farming/life-simulation game developed by Victor Interactive Software INC. This tool exists to help preserve, mod, and study the game's file formats.

**Character files in the game:**
`BOY` (Player), `BASIL` (Parsley), `DAVID` (Nic), `DEERE` (Dia), `EBONY` (Nak), `FLAT` (Flak), `GINA` (Gina), `HAYATO` (Kurt), `KAZIN` (Bob), `KETIE` (Katie), `LYRA` (Lyla), `MARINA` (Harvest Goddess), `MARTHA` (Martha), `RONALD` (Ronald), `RUHN` (Louis), `SARAH` (Gwen), `SHIN` (Joe), `TIM` (Tim), `WALL` (Wallace), `WOOD` (Woody).

---

## 🛠️ Technical Details

- **Language:** C# (.NET Framework 4.0)
- **UI:** Windows Forms with custom dark theme
- **Compression:** Custom LZO implementation (V1 + V2 miniLZO-compatible)
- **Icon:** Embedded resource for cross-machine portability

### Compression Behavior
- Files ≤ 64 bytes → stored RAW (game engine skips these too)
- Files > 64 bytes → compressed if the result is smaller
- Already-compressed formats → stored RAW
- Round-trip verification on every compressed file (safety check)

---

## 👤 Author

**DarthKrayt333**
- GitHub: [@DarthKrayt333](https://github.com/DarthKrayt333)
- Related project: [HMSTHModdingTool](https://github.com/DarthKrayt333/HMSTHModdingTool) — the full modding suite for HMSTH

---

## 🤝 Contributing

Contributions welcome! Feel free to:
- Report bugs via Issues section
- Submit pull requests for improvements
- Share mods you make with the tool

---

## 💬 Community & Support

- Report issues
- Related project → **[HMSTHModdingTool](https://github.com/DarthKrayt333/HMSTHModdingTool)** — the full modding suite for HMSTH

---

**Made with ❤️ for the Harvest Moon modding community**
