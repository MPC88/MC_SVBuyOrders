
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MC_SVBuyOrders
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string pluginGuid = "mc.starvalor.buyorders";
        public const string pluginName = "SV Buy Orders";
        public const string pluginVersion = "0.0.2";
        private const string modSaveFolder = "/MCSVSaveData/";  // /SaveData/ sub folder
        private const string modSaveFilePrefix = "BuyOrders_"; // modSaveFlePrefixNN.dat

        private const int lobbyCode = 1;
        private const int idEnergyCells = 18;
        private const int idVulcanAmmo = 21;
        private const int idCannonAmmo = 20;
        private const int idRailgunAmmo = 53;
        private const int idMissileAmmo = 22;
        private const int idDroneParts = 23;

        internal static PersistentData data;
        internal static ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        public ConfigEntry<KeyCodeSubset> configKey;
        private static bool docked = false;

        public void Awake()
        {
            string pluginfolder = System.IO.Path.GetDirectoryName(GetType().Assembly.Location);
            string bundleName = "mc_svbuyorders";
            Assets.LoadAssets($"{pluginfolder}\\{bundleName}");

            Harmony.CreateAndPatchAll(typeof(Main));

            configKey = Config.Bind<KeyCodeSubset>(
                "Config",
                "Open Buy Order Config",
                KeyCodeSubset.Backspace,
                "Opens configuration menu when docked");
        }

        public void Update()
        {
            if (docked && Input.GetKeyDown((KeyCode)configKey.Value) && !UI.active)
                UI.ShowConfigPanel(data);
        }

        [HarmonyPatch(typeof(MenuControl), nameof(MenuControl.LoadGame))]
        [HarmonyPostfix]
        private static void MenuControlLoadGame_Post()
        {
            LoadData(GameData.gameFileIndex.ToString("00"));
        }

        internal static void LoadData(string saveIndex)
        {
            string modData = Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + saveIndex + ".dat";
            try
            {
                if (!saveIndex.IsNullOrWhiteSpace() && File.Exists(modData))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    FileStream fileStream = File.Open(modData, FileMode.Open);
                    PersistentData loadData = (PersistentData)binaryFormatter.Deserialize(fileStream);
                    fileStream.Close();

                    if (loadData == null)
                        data = new PersistentData();
                    else
                        data = loadData;
                }
                else
                    data = new PersistentData();
            }
            catch
            {
                SideInfo.AddMsg("<color=red>Buy orders mod load failed.</color>");
            }
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.SaveGame))]
        [HarmonyPrefix]
        private static void GameDataSaveGame_Pre()
        {
            SaveData();
        }

        private static void SaveData()
        {
            if (data == null)
                return;

            string tempPath = Application.dataPath + GameData.saveFolderName + modSaveFolder + "BOTemp.dat";

            if (!Directory.Exists(Path.GetDirectoryName(tempPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            FileStream fileStream = File.Create(tempPath);
            binaryFormatter.Serialize(fileStream, data);
            fileStream.Close();

            File.Copy(tempPath, Application.dataPath + GameData.saveFolderName + modSaveFolder + modSaveFilePrefix + GameData.gameFileIndex.ToString("00") + ".dat", true);
            File.Delete(tempPath);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.OpenPanel))]
        [HarmonyPostfix]
        private static void DockingUIOpenPanel_Post(int code)
        {
            if (code != lobbyCode)
            {
                UI.ShowConfigButton(false);
                return;
            }

            if (data == null)
                data = new PersistentData();
            UI.ShowConfigButton(true);
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.StartDockingStation))]
        [HarmonyPostfix]
        private static void DockingUIStartDocking_Post(DockingUI __instance)
        {
            UI.Initialise(__instance);

            if (AccessTools.FieldRefAccess<DockingUI, GameObject>("lobbyPanel")(__instance).activeSelf)
            {
                if (data == null)
                    data = new PersistentData();
                UI.ShowConfigButton(true);
            }
            
            DoOrder(__instance);
            docked = true;
        }

        [HarmonyPatch(typeof(DockingUI), nameof(DockingUI.CloseDockingStation))]
        [HarmonyPrefix]
        private static void DockingUICloseDockingStation_Pre()
        {
            docked = false;
            UI.CloseAndHideAll();
        }

        private static void DoOrder(DockingUI dockingUI)
        {
            // Repair
            if (data.autoRep)
                dockingUI.RepairShip(true);

            if (dockingUI.station.market == null)
                SideInfo.AddMsg("Buy order: No market at this station.");

            DoOrderForItem(idEnergyCells, data.energyCells, dockingUI);
            DoOrderForItem(idVulcanAmmo, data.vulcanAmmo, dockingUI);
            DoOrderForItem(idCannonAmmo, data.cannonAmmo, dockingUI);
            DoOrderForItem(idRailgunAmmo, data.railgunAmmo, dockingUI);
            DoOrderForItem(idMissileAmmo, data.missileAmmo, dockingUI);
            DoOrderForItem(idDroneParts, data.droneParts, dockingUI);
        }

        private static void DoOrderForItem(int itemID, int dataEntry, DockingUI dockingUI)
        {
            if (dataEntry == -1)
                return; // -1 means no action.

            // Get item from player cargo
            CargoSystem playerCS = GameManager.instance.Player.GetComponent<CargoSystem>();
            int cargoItemIndex = -1;
            for (int ciIndex = 0; ciIndex < playerCS.cargo.Count; ciIndex++)
            {
                CargoItem ci = playerCS.cargo[ciIndex];
                if (ci.itemType == 3 && ci.itemID == itemID && ci.qnt > 0)
                {
                    cargoItemIndex = ciIndex;
                    break;
                }
            }
            if (cargoItemIndex == -1 && dataEntry == 0)
                return; // Sell all command, but we don't have any anyway.  No action.
            if (cargoItemIndex > -1 && playerCS.cargo[cargoItemIndex].qnt == dataEntry)
                return; // Quantity is as-per configuration.  No action.

            // Get item from market
            int marketItemIndex = -1;
            Market stationMarket = ((GameObject)AccessTools.Field(typeof(DockingUI), "marketPanel").GetValue(dockingUI)).GetComponent<Market>();
            if(stationMarket.market == null || stationMarket.market.Count == 0)
                stationMarket.market = dockingUI.station.market;
            for (int miIndex = 0; miIndex < stationMarket.market.Count; miIndex++)
            {
                MarketItem mi = stationMarket.market[miIndex];
                if (mi.itemType == 3 && mi.itemID == itemID)
                {
                    marketItemIndex = miIndex;
                    break;
                }
            }
            if (marketItemIndex == -1)
            {
                SideInfo.AddMsg("Buy order: Station does not trade " + ItemDB.GetItem(itemID).itemName);
                return;
            }

            // Now do buy or sell
            if (cargoItemIndex > -1 && playerCS.cargo[cargoItemIndex].qnt > dataEntry)
            {
                // Sell
                CargoItem cargoItem = playerCS.cargo[cargoItemIndex];                
                int sellQnt = playerCS.cargo[cargoItemIndex].qnt - dataEntry;
                GenericCargoItem genericCargoItem = new GenericCargoItem(cargoItem.itemType, cargoItem.itemID, cargoItem.rarity, dockingUI.station.market, null, null, cargoItem.extraData);                
                genericCargoItem.unitPrice = MarketSystem.GetTradeModifier(genericCargoItem.unitPrice, cargoItem.itemType, cargoItem.itemID, true, dockingUI.station.factionIndex, GameManager.instance.Player.GetComponent<SpaceShip>());                
                if(genericCargoItem.unitPrice != -1f)
                {
                    playerCS.RemoveItem(cargoItemIndex, sellQnt);
                    playerCS.credits += genericCargoItem.unitPrice * (float)sellQnt;
                    bool flag2 = false;
                    if (cargoItem.itemType == 3)
                    {
                        if (genericCargoItem.unitPrice * (float)sellQnt > 20000f && PChar.GetRepRank(1) >= 2 && PChar.GetRepRank(2) >= 2 && (PChar.HasPerk(1) || PChar.HasPerk(2)) && GameData.data.difficulty >= 0)
                        {
                            QuestDB.StartQuest(126, 0, false);
                        }
                        flag2 = (cargoItem.itemID == 54);
                        float num = cargoItem.pricePaid;
                        bool flag3 = true;
                        if (num == 0f)
                        {
                            num = ItemDB.GetItem(cargoItem.itemID).basePrice;
                            flag3 = false;
                        }
                        float num2 = genericCargoItem.unitPrice * (float)sellQnt - num * (float)sellQnt;
                        num2 *= 0.5f;
                        if (num2 > 0f)
                        {
                            int num3 = Mathf.RoundToInt(num2 * (1f - (float)PChar.Char.level * 0.01f));
                            if (!flag3)
                            {
                                num3 /= 2;
                            }
                            PChar.EarnXP((float)num3, 4, -1);
                        }
                    }
                    if (!flag2 && MarketSystem.AlterItemStock(dockingUI.station.market, cargoItem.itemType, cargoItem.itemID, cargoItem.rarity, sellQnt) < 0 && cargoItem.rarity >= 1)
                    {
                        MarketItem item = new MarketItem(cargoItem.itemType, cargoItem.itemID, cargoItem.rarity, 1, cargoItem.extraData);
                        dockingUI.station.market.Add(item);
                        MarketSystem.SortMarket(dockingUI.station.market);
                    }
                    playerCS.UpdateAmmoBuffers();
                    Inventory inv = GameObject.FindGameObjectWithTag("MainCanvas").transform.Find("Inventory").GetComponent<Inventory>();
                    inv.transform.parent.Find("PlayerUI").GetComponent<PlayerUIControl>().UpdateUI();
                    SoundSys.PlaySound(20, true);
                }
            }
            else if ((cargoItemIndex > -1 && playerCS.cargo[cargoItemIndex].qnt < dataEntry) ||
                cargoItemIndex == -1 && dataEntry > 0)
            {
                // Select item                
                AccessTools.Field(typeof(Market), "selectedItem").SetValue(stationMarket, marketItemIndex);
                
                // Buy
                int buyQnt = dataEntry;
                if (cargoItemIndex > -1)
                    buyQnt -= playerCS.cargo[cargoItemIndex].qnt;
                if (stationMarket.market[marketItemIndex].stock < buyQnt)
                    buyQnt = stationMarket.market[marketItemIndex].stock;
                stationMarket.BuyMarketItem(buyQnt);

                // Reset market selectiosn to avoid shenanigans from external manipulation
                AccessTools.Field(typeof(Market), "selectedItem").SetValue(stationMarket, -1);
                if (AccessTools.FieldRefAccess<DockingUI, GameObject>("lobbyPanel")(dockingUI).activeSelf ||
                    AccessTools.FieldRefAccess<DockingUI, GameObject>("craftingPanel")(dockingUI).activeSelf)
                    AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive)).Invoke(
                        AccessTools.Field(typeof(Market), "shipDataScreen").GetValue(stationMarket), new object[] { false });
            }
        }
    }

    [Serializable]
    public class PersistentData
    {
        public bool autoRep = false;
        public int energyCells = -1;
        public int vulcanAmmo = -1;
        public int cannonAmmo = -1;
        public int railgunAmmo = -1;
        public int missileAmmo = -1;
        public int droneParts = -1;
    }
}
