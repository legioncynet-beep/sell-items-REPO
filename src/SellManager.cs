using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace RepoSellMod
{
    public enum ItemCategory
    {
        Unknown,
        Weapon,
        Upgrade,
        Consumable,
        Other
    }

    public class SellManager : MonoBehaviour
    {
        public static SellManager Instance { get; private set; }

        // Cache: itemName (lowercase) → purchase price
        private readonly Dictionary<string, int> _priceCache = new Dictionary<string, int>();

        private GUIStyle _hintStyle;
        private bool _styleInit;
        private bool _updateLogged;
        private GameObject _pendingSell;
        private float _pendingTimer;

        // ── Unity lifecycle ────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Plugin.Log.LogInfo("[SellMod] SellManager.Awake() — OK");
        }

        private void Update()
        {
            if (!_updateLogged)
            {
                Plugin.Log.LogInfo("[SellMod] SellManager.Update() is running");
                _updateLogged = true;
            }

            if (!IsInLobby())
            {
                _pendingSell = null;
                return;
            }

            // Reset pending sell if the item was dropped
            if (_pendingSell != null)
            {
                var held = GetHeldItem();
                if (held != _pendingSell)
                    _pendingSell = null;
            }

            // Confirmation timeout
            if (_pendingSell != null)
            {
                _pendingTimer -= Time.deltaTime;
                if (_pendingTimer <= 0f)
                {
                    _pendingSell = null;
                    Plugin.Log.LogInfo("[SellMod] Confirmation timed out");
                }
            }

            // Enter — confirm sell
            if (_pendingSell != null && Input.GetKeyDown(KeyCode.Return))
            {
                var item = _pendingSell;
                _pendingSell = null;
                TrySell(item);
                return;
            }

            if (Input.GetKeyDown(Plugin.SellKey.Value))
            {
                // Press sell key again — cancel
                if (_pendingSell != null)
                {
                    _pendingSell = null;
                    SemiFunc.UIFocusText("Sale cancelled.", Color.white, Color.white, 1.5f);
                    return;
                }

                // First press — request confirmation
                var heldItem = GetHeldItem();
                if (heldItem == null) return;

                if (!IsSellable(heldItem, out string reason))
                {
                    Plugin.Log.LogInfo($"[SellMod] Cannot sell: {reason}");
                    SemiFunc.UIFocusText($"Cannot sell: {reason}", Color.red, Color.white, 2f);
                    return;
                }

                int refund = GetRefundAmount(heldItem);
                _pendingSell = heldItem;
                _pendingTimer = 5f;

                string confirm = refund > 0
                    ? $"Sell for {refund}$? [Enter] yes  [X] cancel"
                    : "Discard item? [Enter] yes  [X] cancel";
                SemiFunc.UIFocusText(confirm, Color.yellow, Color.white, 5f);
                Plugin.Log.LogInfo($"[SellMod] Awaiting confirmation: {heldItem.name}");
            }
        }

        private void OnGUI()
        {
            try
            {
                InitStyle();

                // ── Debug overlay ──────────
                bool inLobby = IsInLobby();
                var grabber = PhysGrabber.instance;
                bool grabbed = grabber != null && grabber.grabbed;
                var physObj = grabbed
                    ? (_grabbedObjField?.GetValue(grabber) as PhysGrabObject)
                    : null;
                if (Plugin.ShowDebugOverlay.Value)
                {
                    string debug = $"[SellMod] lobby:{inLobby} grabbed:{grabbed} item:{physObj?.gameObject?.name ?? "none"} pending:{_pendingSell?.name ?? "none"}";
                    GUI.Label(new Rect(10, 10, 700, 24), debug, _hintStyle);
                }
                // ─────────────────────────────────────────────────────

                if (!Plugin.ShowSellHint.Value) return;
                if (!inLobby) return;
                if (physObj == null) return;

                string label;
                if (_pendingSell == physObj.gameObject)
                {
                    label = "[Enter] confirm sell   [X] cancel";
                }
                else if (IsSellable(physObj.gameObject, out _))
                {
                    int refund = GetRefundAmount(physObj.gameObject);
                    label = refund > 0
                        ? $"[{Plugin.SellKey.Value}] Sell ({refund}$)"
                        : $"[{Plugin.SellKey.Value}] Discard (0$)";
                }
                else return;

                float w = 340f, h = 32f;
                float x = (Screen.width - w) / 2f;
                float y = Screen.height - h - 80f;

                GUI.Label(new Rect(x - 1, y + 1, w, h), label, ShadowStyle());
                GUI.Label(new Rect(x, y, w, h), label, _hintStyle);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"[SellMod] OnGUI exception: {e}");
            }
        }

        // ── Core logic ─────────────────────────────────────────────

        public bool TrySell(GameObject item)
        {
            if (!IsSellable(item, out string reason))
            {
                Plugin.Log.LogInfo($"[SellMod] Cannot sell '{item.name}': {reason}");
                SemiFunc.UIFocusText($"Cannot sell: {reason}", Color.red, Color.white, 2f);
                return false;
            }

            int refund = GetRefundAmount(item);
            if (refund > 0) GiveMoneyToPlayer(refund);

            // Get item name before destroying the object
            var attrs = item.GetComponentInParent<ItemAttributes>(true)
                     ?? item.GetComponent<ItemAttributes>();
            string itemName = attrs?.item?.name;

            string msg = refund > 0 ? $"Sold for {refund}$!" : "Item discarded.";
            SemiFunc.UIFocusText(msg, Color.green, Color.white, 2f);
            Plugin.Log.LogInfo($"[SellMod] Sold '{item.name}' for {refund}$");

            DestroyItem(item);

            if (!string.IsNullOrEmpty(itemName))
                RemoveFromStats(itemName);
            else
                Plugin.Log.LogWarning($"[SellMod] Could not get itemName for {item.name}");

            return true;
        }

        public bool IsSellable(GameObject item, out string reason)
        {
            reason = string.Empty;
            if (item == null) { reason = "null"; return false; }

            // Never sell players
            if (item.GetComponentInParent<PlayerAvatar>(true) != null)
            {
                reason = "that's a player";
                return false;
            }

            // Only allow selling items that have actually been purchased
            var attrs = item.GetComponentInParent<ItemAttributes>(true)
                     ?? item.GetComponent<ItemAttributes>();
            string itemName = attrs?.item?.name;

            if (!string.IsNullOrEmpty(itemName))
            {
                var stats = StatsManager.instance;
                if (stats != null)
                {
                    bool purchased = stats.itemsPurchased.ContainsKey(itemName)
                                  && stats.itemsPurchased[itemName] > 0;
                    if (!purchased)
                    {
                        reason = "item was not purchased";
                        return false;
                    }
                }
            }

            if (Plugin.SellAll.Value) return true;

            var cat = GetCategory(item);
            switch (cat)
            {
                case ItemCategory.Weapon when Plugin.SellWeapons.Value: return true;
                case ItemCategory.Upgrade when Plugin.SellUpgrades.Value: return true;
                case ItemCategory.Consumable when Plugin.SellConsumables.Value: return true;
                case ItemCategory.Weapon:
                    reason = "weapon selling is disabled"; return false;
                case ItemCategory.Upgrade:
                    reason = "upgrade selling is disabled"; return false;
                case ItemCategory.Consumable:
                    reason = "consumable selling is disabled"; return false;
                default:
                    reason = "unknown category, enable SellAll"; return false;
            }
        }

        // ── Item categorization via SemiFunc.itemType ──────────────

        public static ItemCategory GetCategory(GameObject item)
        {
            if (item == null) return ItemCategory.Unknown;

            // 1. ItemAttributes.item.itemType — real game enum
            var attrs = item.GetComponentInChildren<ItemAttributes>(true)
                     ?? item.GetComponentInParent<ItemAttributes>(true);

            if (attrs?.item != null)
            {
                var t = attrs.item.itemType;

                if (t == SemiFunc.itemType.item_upgrade)
                    return ItemCategory.Upgrade;

                if (t == SemiFunc.itemType.healthPack)
                    return ItemCategory.Consumable;

                if (t == SemiFunc.itemType.power_crystal)
                    return ItemCategory.Consumable;

                // Everything else (weapons, tools) counts as Weapon
                return ItemCategory.Weapon;
            }

            // 2. Fallback by name (modded items without ItemAttributes)
            string name = item.name.ToLowerInvariant().Replace("(clone)", "").Trim();

            if (ContainsAny(name, "upgrade", "module", "boost", "enhancement", "chip"))
                return ItemCategory.Upgrade;

            if (ContainsAny(name, "medkit", "health", "crystal", "grenade",
                                  "bandage", "stim", "pack", "ammo"))
                return ItemCategory.Consumable;

            if (ContainsAny(name, "gun", "rifle", "pistol", "shotgun", "weapon",
                                  "sword", "blade", "melee", "bow", "launcher"))
                return ItemCategory.Weapon;

            // 3. Component names (modded items without ItemAttributes)
            foreach (var comp in item.GetComponents<Component>())
            {
                if (comp == null) continue;
                string cname = comp.GetType().Name.ToLowerInvariant();
                if (ContainsAny(cname, "weapon", "gun", "shoot", "fire"))
                    return ItemCategory.Weapon;
                if (ContainsAny(cname, "upgrade", "module", "boost"))
                    return ItemCategory.Upgrade;
            }

            return ItemCategory.Unknown;
        }

        // ── Pricing ────────────────────────────────────────────────

        public int GetRefundAmount(GameObject item) =>
            Mathf.RoundToInt(GetItemPrice(item) * Plugin.SellRefundRatio.Value);

        public int GetItemPrice(GameObject item)
        {
            if (item == null) return 0;

            string key = GetItemKey(item);
            if (_priceCache.TryGetValue(key, out int cached)) return cached;

            var attrs = item.GetComponentInChildren<ItemAttributes>(true)
                     ?? item.GetComponentInParent<ItemAttributes>(true);

            if (attrs?.item != null)
            {
                // Reproduce ShopManager formula:
                //   price = item.value.valueMax / 1000f * itemValueMultiplier (default 4f)
                int price = Mathf.CeilToInt(attrs.item.value.valueMax / 1000f * 4f);
                if (price > 0)
                {
                    _priceCache[key] = price;
                    return price;
                }
            }

            return 0;
        }

        public void RegisterItemPrice(string itemName, int price)
        {
            _priceCache[itemName.ToLowerInvariant()] = price;
            Plugin.Log.LogInfo($"[SellMod] Price registered: {itemName} = {price}$");
        }

        // ── Phase check ────────────────────────────────────────────

        /// <summary>
        /// Selling is only allowed in the lobby truck (between levels).
        /// Uses SemiFunc.RunIsLobby() — official R.E.P.O. API.
        /// GameDirector state check blocks selling during scene transitions.
        /// </summary>
        private static bool IsInLobby()
        {
            try
            {
                if (GameDirector.instance == null) return false;
                if (GameDirector.instance.currentState != GameDirector.gameState.Main)
                    return false;

                return SemiFunc.RunIsLobby();
            }
            catch { return false; }
        }

        // ── Game helpers ───────────────────────────────────────────

        private static readonly System.Reflection.FieldInfo _grabbedObjField =
            typeof(PhysGrabber).GetField("grabbedPhysGrabObject",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

        private static GameObject GetHeldItem()
        {
            var grabber = PhysGrabber.instance;
            if (grabber == null || !grabber.grabbed) return null;

            var physObj = _grabbedObjField?.GetValue(grabber) as PhysGrabObject;
            return physObj?.gameObject;
        }

        private static void GiveMoneyToPlayer(int amount)
        {
            // StatSetRunCurrency goes through PunManager and syncs across the network
            int current = SemiFunc.StatGetRunCurrency();
            SemiFunc.StatSetRunCurrency(current + amount);
            SemiFunc.ShopUpdateCost(); // refresh shop cost UI
            Plugin.Log.LogInfo($"[SellMod] +{amount}$ (was {current}$, now {current + amount}$)");
        }

        private static void DestroyItem(GameObject item)
        {
            var physObj = item.GetComponentInParent<PhysGrabObject>(true)
                       ?? item.GetComponent<PhysGrabObject>();
            GameObject target = physObj != null ? physObj.gameObject : item;

            var photonView = target.GetComponent<PhotonView>();

            if (photonView != null && PhotonNetwork.IsConnected)
            {
                // Transfer ownership before destroying if we don't own it
                if (!photonView.IsMine)
                    photonView.TransferOwnership(PhotonNetwork.MasterClient);

                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(photonView);
                    Plugin.Log.LogInfo("[SellMod] Item destroyed via PhotonNetwork.Destroy (host)");
                }
                else
                {
                    // Client: ask host to destroy by viewID
                    SellManager.Instance.GetComponent<PhotonView>()?.RPC(
                        nameof(RequestDestroyRPC), RpcTarget.MasterClient, photonView.ViewID);
                    Plugin.Log.LogInfo($"[SellMod] RPC destroy request sent to host, viewID:{photonView.ViewID}");
                }
                return;
            }

            // Singleplayer or no Photon connection
            Destroy(target);
            Plugin.Log.LogInfo("[SellMod] Item destroyed locally (no PhotonView)");
        }

        [PunRPC]
        public void RequestDestroyRPC(int viewID)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            var pv = PhotonView.Find(viewID);
            if (pv != null)
            {
                Plugin.Log.LogInfo($"[SellMod] Host destroying viewID:{viewID} on client request");
                PhotonNetwork.Destroy(pv);
            }
        }

        private static void RemoveFromStats(string itemName)
        {
            try
            {
                var stats = StatsManager.instance;
                if (stats == null) return;

                if (stats.itemsPurchased.ContainsKey(itemName) && stats.itemsPurchased[itemName] > 0)
                {
                    stats.itemsPurchased[itemName]--;
                    Plugin.Log.LogInfo($"[SellMod] Purchase count '{itemName}': {stats.itemsPurchased[itemName] + 1} → {stats.itemsPurchased[itemName]}");
                }
                else
                {
                    Plugin.Log.LogWarning($"[SellMod] '{itemName}' not found in itemsPurchased or already 0");
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"[SellMod] RemoveFromStats: {e.Message}");
            }
        }

        private static string GetItemKey(GameObject item) =>
            item.name.ToLowerInvariant().Replace("(clone)", "").Trim();

        private static bool ContainsAny(string source, params string[] kw) =>
            kw.Any(source.Contains);

        // ── GUI styles ─────────────────────────────────────────────

        private void InitStyle()
        {
            if (_styleInit) return;
            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            _styleInit = true;
        }

        private GUIStyle ShadowStyle()
        {
            var s = new GUIStyle(_hintStyle);
            s.normal.textColor = Color.black;
            return s;
        }
    }
}
