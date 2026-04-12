# SellItems

A BepInEx 5 mod for **R.E.P.O.** — sell unwanted items in the lobby truck between levels and get your money back.

## Features

| Feature | Description |
|---------|-------------|
| Sell items | Hold an item → press `X` → confirm with `Enter` |
| Configurable refund | 50% by default, adjustable in config |
| On-screen hint | `[X] Sell (250$)` appears while holding a sellable item |
| Category filter | Toggle weapons, upgrades, and consumables independently |
| Mod support | Works with any item that has `ItemAttributes` or is detected by name/component |
| Purchase check | Only items actually bought in the shop can be sold |
| Public API | Other mods can register item prices and trigger sells programmatically |
| Multiplayer | Item destruction synced via `PhotonNetwork.Destroy` |

## Installation

### With mod manager (r2modman / Thunderstore Mod Manager)
1. Search for **SellItems** in the mod browser
2. Click **Install**
3. Launch the game through the mod manager

### Manual
1. Install [BepInEx 5.x](https://thunderstore.io/c/repo/p/BepInEx/BepInExPack/)
2. Place `RepoSellModMod.dll` into `BepInEx/plugins/`
3. Launch the game

## How to use

1. Buy an item in the shop.
2. Return to the lobby truck.
3. Pick up the item.
4. Press **X** — a confirmation prompt appears.
5. Press **Enter** to confirm, or **X** again to cancel.

> Selling is only available in the lobby truck. It is blocked during shop browsing, scene transitions, and active levels.

## API for mod developers

If your mod adds custom items, register their price so the refund is accurate:

```csharp
using RepoSellMod;

// In your plugin's Awake():
SellModAPI.RegisterItemPrice("MyCustomGun", 800);

// Check if an item can be sold:
bool ok = SellModAPI.IsSellable(gameObject);

// Get item category:
ItemCategory cat = SellModAPI.GetCategory(gameObject); // Weapon / Upgrade / …

// Get refund amount:
int refund = SellModAPI.GetRefundAmount(gameObject);

// Sell programmatically:
bool sold = SellModAPI.TrySell(gameObject);
```

## Multiplayer

- Install on the **host** for the mod to work
- Recommended: all players install for full compatibility
- Works in singleplayer without any restrictions

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Item can't be sold | Enable the relevant category in config, or set `SellAll = true` |
| Refund is 0$ | Item wasn't bought through the shop this session; register price via API |
| Hint doesn't appear | Check `ShowSellHint = true` and that you're in the lobby truck |
| Mod not loading | Verify BepInEx 5 (not 6) and check `BepInEx/LogOutput.log` |
| Items reappear after level | Check `LogOutput.log` for `Purchase count` lines to confirm stat update |