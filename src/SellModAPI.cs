using UnityEngine;

namespace RepoSellMod
{
    /// <summary>
    /// Public API for third-party mods to integrate with RepoSellMod.
    ///
    /// Other mods can:
    ///   1. Register their item's price for accurate refund calculation.
    ///   2. Programmatically sell an item.
    ///   3. Check whether an item is sellable.
    ///
    /// Example usage from another mod:
    ///   SellModAPI.RegisterItemPrice("MyAwesomeGun", 500);
    ///   bool ok = SellModAPI.TrySell(myGunGameObject);
    /// </summary>
    public static class SellModAPI
    {
        // ── Registration ───────────────────────────────────────────

        /// <summary>
        /// Register the price of an item by its name (item.name without "(Clone)").
        /// Call this from your mod's Awake() or Start().
        /// </summary>
        public static void RegisterItemPrice(string itemName, int price)
        {
            if (SellManager.Instance == null)
            {
                Plugin.Log.LogWarning("[SellMod API] SellManager is not yet initialized.");
                return;
            }
            SellManager.Instance.RegisterItemPrice(itemName, price);
        }

        // ── Selling ────────────────────────────────────────────────

        /// <summary>
        /// Programmatically sell an item.
        /// Returns true if the sale was successful.
        /// </summary>
        public static bool TrySell(GameObject item)
        {
            if (SellManager.Instance == null) return false;
            return SellManager.Instance.TrySell(item);
        }

        // ── Queries ────────────────────────────────────────────────

        /// <summary>
        /// Check whether selling this item is allowed by the current config.
        /// </summary>
        public static bool IsSellable(GameObject item)
        {
            if (SellManager.Instance == null) return false;
            return SellManager.Instance.IsSellable(item, out _);
        }

        /// <summary>
        /// Get the item's category (Weapon / Upgrade / Consumable / Other / Unknown).
        /// </summary>
        public static ItemCategory GetCategory(GameObject item) =>
            SellManager.GetCategory(item);

        /// <summary>
        /// Get the refund amount the player would receive for selling this item.
        /// </summary>
        public static int GetRefundAmount(GameObject item)
        {
            if (SellManager.Instance == null) return 0;
            return SellManager.Instance.GetRefundAmount(item);
        }

        /// <summary>
        /// Get the cached/registered base price of an item.
        /// </summary>
        public static int GetItemPrice(GameObject item)
        {
            if (SellManager.Instance == null) return 0;
            return SellManager.Instance.GetItemPrice(item);
        }
    }
}
