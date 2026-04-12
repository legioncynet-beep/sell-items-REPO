using HarmonyLib;
using UnityEngine;

namespace RepoSellMod
{
    // ─────────────────────────────────────────────────────────────────────────
    // Patch ShopManager.ShopInitialize:
    // Called when the shop opens. At this point all item lists are populated,
    // so we cache prices for accurate refund calculation.
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(ShopManager), nameof(ShopManager.ShopInitialize))]
    internal static class ShopManager_ShopInitialize_Patch
    {
        private static void Postfix(ShopManager __instance)
        {
            if (SellManager.Instance == null) return;

            CacheItemList(__instance.potentialItems);
            CacheItemList(__instance.potentialItemUpgrades);
            CacheItemList(__instance.potentialItemConsumables);
            CacheItemList(__instance.potentialItemHealthPacks);

            Plugin.Log.LogDebug("[SellMod] Price cache updated on shop open.");
        }

        private static void CacheItemList(System.Collections.Generic.List<Item> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null) continue;
                // Reproduce ShopManager price formula (itemValueMultiplier defaults to 4f)
                int price = Mathf.CeilToInt(item.value.valueMax / 1000f * 4f);
                if (price > 0)
                    SellManager.Instance.RegisterItemPrice(item.name, price);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Patch ShopManager.ShoppingListItemAdd:
    // Called when a player places an item into the extraction/purchase zone.
    // Most accurate moment to record the price of a specific item instance.
    // ─────────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(ShopManager), nameof(ShopManager.ShoppingListItemAdd))]
    internal static class ShopManager_ShoppingListItemAdd_Patch
    {
        private static void Prefix(ItemAttributes item)
        {
            if (item?.item == null || SellManager.Instance == null) return;

            int price = Mathf.CeilToInt(item.item.value.valueMax / 1000f * 4f);
            if (price > 0)
            {
                SellManager.Instance.RegisterItemPrice(item.item.name, price);
                Plugin.Log.LogDebug($"[SellMod] Added to cart: {item.item.name} = {price}$");
            }
        }
    }
}
