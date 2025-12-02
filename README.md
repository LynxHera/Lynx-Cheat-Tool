# Lynx Cheat Tool

![CS2](https://img.shields.io/badge/Game-CS2-orange?style=for-the-badge&logo=counter-strike)
![Platform](https://img.shields.io/badge/Platform-CounterStrikeSharp-blue?style=for-the-badge)
![Runtime](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**Lynx Cheat Tool** is a comprehensive management and entertainment plugin developed for Counter-Strike 2 servers, based on **CounterStrikeSharp**. It includes advanced features that allow server administrators to troll or test players.

---

## 🌟 Features

### 🎯 Aimbot
*   **Automatic Lock:** Automatically locks onto enemy heads.
*   **Sticky Targeting:** Once locked, it stays locked even if the target moves (360° FOV).
*   **Smart Targeting:** Ignores teammates.
*   **3D Calculation:** Performs real-time head tracking based on player movement and position.

### 🧱 Wallhack (ESP)
*   **Glow Effect:** Shows enemies behind walls with a red glow.
*   **Stability:** Seamless visuals with a double entity system (ModelRelay + ModelGlow).
*   **Smart Render:** Visible only to the enemy team; clears dead players and spectators.

### 💀 Magic Bullet (One Shot)
*   **Instant Kill:** Instantly kills wherever the bullet hits (leg, arm, body).
*   **Headshot Icon:** Always appears as a **Headshot** 🎯 in the killfeed.
*   **Silent Death:** The victim drops dead without realizing what happened.

### 🔫 No Recoil
*   **Laser Accuracy:** Removes all weapon recoil and visual kick.
*   **No CVar:** Does not rely on server CVars like `weapon_recoil_scale`.
*   **Perfect Spray:** Bullets go exactly where your crosshair is.

### ⚡ No Flash
*   **Total Immunity:** Flashbangs have zero effect on you.
*   **Instant Recovery:** Even if flashed, the effect is removed instantly.
*   **Toggleable:** Can be enabled/disabled per player.

### 🌟 Frag Changer (Killfeed Show)
Creates a visual feast in the killfeed (top right death notices). When you kill someone, **ALL** or **SELECTED** icons below will appear:
*   🎯 **Headshot**
*   🧱 **Penetrated** (Wallbang)
*   ☁️ **Thru Smoke**
*   🕶️ **Blind**
*   🚫 **Noscope**
*   🦅 **AttackerAir** (Airborne/Jumping)
*   ☠️ **Domination / Revenge / Wipe**

> **Note:** You can configure exactly which icons appear for each player individually via the detail menu!

---

## 🎮 Commands

All commands require `@css/root` permission.

| Command | Description |
| :--- | :--- |
| `!cheats` / `css_cheats` | Opens the **Main Menu** (Access all features here). |
| `!aimbot` | Directly opens the **Aimbot** menu. |
| `!wallhack` | Directly opens the **Wallhack** menu. |
| `!magicbullet` | Directly opens the **Magic Bullet** menu. |
| `!fragchanger` | Directly opens the **Frag Changer** menu. |
| `!norecoil` | Directly opens the **No Recoil** menu. |
| `!noflash` | Directly opens the **No Flash** menu. |

---

## 📋 Menu Usage (WASD)

The plugin uses the in-game **WASD Menu** system. After typing a command in chat:

*   **W / S:** Navigate Up / Down
*   **A / D:** Enter Menu / Go Back
*   **E:** Confirm Selection / Toggle Feature

### 👥 "Toggle All" Feature
With the **"👥 Toggle All"** option at the top of each menu, you can activate or deactivate that feature for all players on the server with a single click.

---

## 📦 Installation

1.  **Requirements:**
    *   [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (Latest version)
    *   [CS2MenuManager](https://github.com/schwarper/CS2MenuManager) (Must be installed in addons folder)
    *   .NET 8.0 Runtime (Must be installed on the server)

2.  **Setup:**
    *   Drop the `LynxCheatToolAimbot.dll` file into your server's `addons/counterstrikesharp/plugins/LynxCheatToolAimbot/` folder.
    *   Restart the server or use the `css_plugins reload LynxCheatToolAimbot` command.

---

## ⚠️ Disclaimer

This plugin is for **ENTERTAINMENT and TESTING** purposes only.
*   Use only on servers you own or have permission to manage.
*   The developer is not responsible for any bans that may occur.

---

## 👨‍💻 Developer

Developed with ❤️ by **LynxHera**.

*   **Wallhack Method:** ESP-Players-GoldKingZ
*   **Menu System:** CS2MenuManager by Schwarper
