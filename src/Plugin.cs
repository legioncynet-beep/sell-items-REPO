using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace RepoSellMod
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        public static ConfigEntry<KeyCode> SellKey { get; private set; }
        public static ConfigEntry<float> SellRefundRatio { get; private set; }
        public static ConfigEntry<bool> ShowSellHint { get; private set; }
        public static ConfigEntry<bool> SellWeapons { get; private set; }
        public static ConfigEntry<bool> SellUpgrades { get; private set; }
        public static ConfigEntry<bool> SellConsumables { get; private set; }
        public static ConfigEntry<bool> SellAll { get; private set; }
        public static ConfigEntry<bool> ShowDebugOverlay { get; private set; }

        private readonly Harmony _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo("=== RepoSellMod: Awake() started ===");

            BindConfig();
            Log.LogInfo($"Config loaded. Sell key: [{SellKey.Value}], Refund: {SellRefundRatio.Value * 100:0}%");

            _harmony.PatchAll();
            Log.LogInfo("Harmony patches applied.");

            gameObject.AddComponent<SellManager>();
            Log.LogInfo("SellManager added.");

            Log.LogInfo($"=== {MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded! ===");
        }

        private void BindConfig()
        {
            SellKey = Config.Bind(
                "General", "SellKey", KeyCode.X,
                "Key to initiate selling the held item");

            SellRefundRatio = Config.Bind(
                "General", "SellRefundRatio", 0.5f,
                new ConfigDescription(
                    "Fraction of the item price returned on sell (0.0–1.0). 0.5 = 50%",
                    new AcceptableValueRange<float>(0f, 1f)));

            ShowSellHint = Config.Bind(
                "UI", "ShowSellHint", true,
                "Show the [X] Sell hint while holding a sellable item");

            SellWeapons = Config.Bind(
                "Items", "SellWeapons", true,
                "Allow selling weapons");

            SellUpgrades = Config.Bind(
                "Items", "SellUpgrades", true,
                "Allow selling upgrades");

            SellConsumables = Config.Bind(
                "Items", "SellConsumables", false,
                "Allow selling consumables");

            SellAll = Config.Bind(
                "Items", "SellAll", false,
                "Allow selling ANY item (overrides all category flags above)");

            ShowDebugOverlay = Config.Bind(
                "UI", "ShowDebugOverlay", false,
                "Show the debug overlay in the top-left corner");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
