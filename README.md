# REPO Sell Items

A BepInEx 5 mod for **R.E.P.O.** — sell unwanted items in the lobby truck between levels and get your money back.

---

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

---

## Installation

1. Install [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) (5.x Mono build for Unity).
2. Launch the game once so BepInEx generates its folder structure.
3. Copy `RepoSellMod.dll` to:
   ```
   REPO/BepInEx/plugins/RepoSellMod/RepoSellMod.dll
   ```
4. Launch the game.

---

## How to use

1. Buy an item in the shop.
2. Return to the lobby truck.
3. Pick up the item (right-click / grab).
4. Press **X** — a confirmation prompt appears.
5. Press **Enter** to confirm, or **X** again to cancel.

> Selling is only available in the lobby truck. It is blocked during shop browsing, scene transitions, and active levels.

---

## Configuration

Config file is created automatically at:
```
REPO/BepInEx/config/ru.example.repo.sellmod.cfg
```

```ini
[General]
# Key to initiate selling (default: X)
SellKey = X

# Refund fraction: 0.5 = 50%, 1.0 = full refund
SellRefundRatio = 0.5

[UI]
# Show the sell hint while holding an item
ShowSellHint = true

[Items]
# Allow selling weapons
SellWeapons = true

# Allow selling upgrades
SellUpgrades = true

# Allow selling consumables (medkits, etc.)
SellConsumables = false

# Allow selling ANY item (overrides all flags above)
SellAll = false
```

---

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

### Automatic detection (no registration needed)

The mod attempts to determine the price automatically via:
1. `ItemAttributes.item.value.valueMax` using the ShopManager formula
2. Price cache populated by Harmony patches on `ShopManager.ShopInitialize` and `ShopManager.ShoppingListItemAdd`

Category is determined by:
- `ItemAttributes.item.itemType` (real game enum via `SemiFunc.itemType`)
- Object name keywords (`gun`, `rifle`, `upgrade`, …)
- Component type names (for modded items without `ItemAttributes`)

---

## Building from source

```powershell
dotnet build -c Release
```

The DLL is automatically copied to `BepInEx/plugins/RepoSellMod/` after a successful build.

### Requirements
- .NET SDK 6+
- BepInEx 5.x (Mono)
- R.E.P.O. (Steam)
- Paths in `.csproj` must point to your local BepInEx and game folders

---

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Item can't be sold | Enable the relevant category in config, or set `SellAll = true` |
| Refund is 0$ | Item wasn't bought through the shop this session; register price via API |
| Hint doesn't appear | Check `ShowSellHint = true` and that you're in the lobby truck |
| Mod not loading | Verify BepInEx 5 (not 6) and check `BepInEx/LogOutput.log` |
| Items reappear after level | Check `LogOutput.log` for `Purchase count` lines to confirm stat update |

---

## License

MIT — do whatever you want, attribution appreciated.
