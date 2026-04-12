# Changelog

## 1.0.0
- Initial release
- Sell held items with configurable refund percentage (default 50%)
- Category filter: weapons, upgrades, consumables
- Mod support via public SellModAPI
- Confirmation prompt before selling (press X to initiate, Enter to confirm, X again to cancel)
- Selling only allowed in the lobby truck — blocked during shop, transitions, and active levels
- Items are only sellable if they were actually purchased in the shop
- Item destruction synced across all clients via PhotonNetwork.Destroy
- Debug overlay toggle in config (UI.ShowDebugOverlay)
- On-screen hint shows confirmation state
