using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace MC_SVBuyOrders
{
    internal class UI
    {
        internal static bool active = false;
        private static GameObject btnConfig;
        private static GameObject pnlMain;
        private static Toggle tglAutoRep;
        private static InputField inputECells;
        private static InputField inputVulcan;
        private static InputField inputCannon;
        private static InputField inputRail;
        private static InputField inputMissile;
        private static InputField inputDrone;

        internal static void Initialise(DockingUI dockingUI)
        {
            if (pnlMain != null && btnConfig != null)
                return;

            // Open panel button
            Transform lobbyPanel = ((GameObject)AccessTools.FieldRefAccess<DockingUI, GameObject>("lobbyPanel")(dockingUI)).transform;
            GameObject src = lobbyPanel.GetChild(1).GetChild(0).GetChild(2).gameObject;
            btnConfig = GameObject.Instantiate(src);
            btnConfig.name = "BtnConfigAutoBuy";
            btnConfig.SetActive(true);
            btnConfig.GetComponentInChildren<Text>().text = "Auto Trade";
            RectTransform rt = btnConfig.GetComponent<RectTransform>();
            ButtonClickedEvent configBCE = new Button.ButtonClickedEvent();
            configBCE.AddListener(btnConfig_Click);
            btnConfig.GetComponentInChildren<Button>().onClick = configBCE;
            btnConfig.transform.SetParent(lobbyPanel, true);
            btnConfig.transform.localScale = src.transform.localScale;
            RectTransform lobPanRect = lobbyPanel.GetComponent<RectTransform>();
            btnConfig.transform.localPosition = new Vector3(-495, 309, 0);
            btnConfig.SetActive(false);

            // Get mod UI game objects
            pnlMain = GameObject.Instantiate(Assets.pnlMain);
            pnlMain.transform.SetParent(((GameObject)AccessTools.Field(typeof(DockingUI), "lobbyPanel").GetValue(dockingUI)).transform);
            pnlMain.transform.localPosition = new Vector3(0,0,0);
            pnlMain.transform.localScale = Vector3.one;
            pnlMain.SetActive(false);
            tglAutoRep = pnlMain.transform.Find("mc_svbuyorderautorep").gameObject.GetComponentInChildren<Toggle>();
            inputECells = pnlMain.transform.Find("mc_svbuyorderECellsIn").gameObject.GetComponentInChildren<InputField>();
            inputVulcan = pnlMain.transform.Find("mc_svbuyorderVulcanIn").gameObject.GetComponentInChildren<InputField>();
            inputCannon = pnlMain.transform.Find("mc_svbuyorderCannonIn").gameObject.GetComponentInChildren<InputField>();
            inputRail = pnlMain.transform.Find("mc_svbuyorderRailIn").gameObject.GetComponentInChildren<InputField>();
            inputMissile = pnlMain.transform.Find("mc_svbuyorderMissileIn").gameObject.GetComponentInChildren<InputField>();
            inputDrone = pnlMain.transform.Find("mc_svbuyorderDroneIn").gameObject.GetComponentInChildren<InputField>();

            // Setup button events
            ButtonClickedEvent cancelBCE = new ButtonClickedEvent();
            cancelBCE.AddListener(btnCancel_Click);
            pnlMain.transform.Find("mc_svbuyorderCancel").gameObject.GetComponent<Button>().onClick = cancelBCE;

            ButtonClickedEvent confirmBCE = new ButtonClickedEvent();
            confirmBCE.AddListener(btnConfirm_Click);
            pnlMain.transform.Find("mc_svbuyorderConfirm").gameObject.GetComponent<Button>().onClick = confirmBCE;
        }

        internal static void ShowConfigButton(bool state)
        {   
            if(btnConfig != null)
                btnConfig.SetActive(state);
        }

        internal static void CloseAndHideAll()
        {
            CloseConfigPanel();
            ShowConfigButton(false);
        }

        internal static void ShowConfigPanel(PersistentData data)
        {
            if (data == null)
                return;

            tglAutoRep.SetIsOnWithoutNotify(data.autoRep);
            inputECells.text = data.energyCells.ToString();
            inputVulcan.text = data.vulcanAmmo.ToString();
            inputCannon.text = data.cannonAmmo.ToString();
            inputRail.text = data.railgunAmmo.ToString();
            inputMissile.text = data.missileAmmo.ToString();
            inputDrone.text = data.droneParts.ToString();

            pnlMain.SetActive(true);
            active = true;
        }

        private static void CloseConfigPanel()
        {
            if (pnlMain != null)
                pnlMain.SetActive(false);
            active = false;
        }

        private static void btnConfig_Click()
        {
            ShowConfigPanel(Main.data);
        }

        private static void btnCancel_Click()
        {
            CloseConfigPanel();
        }

        private static void btnConfirm_Click()
        {
            try
            {
                Main.data.autoRep = tglAutoRep.isOn;
                
                int tmp = Int32.Parse(inputECells.text);
                if (tmp < -1)
                    throw new ArgumentOutOfRangeException();
                Main.data.energyCells = tmp;

                tmp = Int32.Parse(inputVulcan.text);
                if (tmp < -1)
                    throw new ArgumentOutOfRangeException();
                Main.data.vulcanAmmo = tmp;

                tmp = Int32.Parse(inputCannon.text);
                if (tmp < -1)
                    throw new ArgumentOutOfRangeException();
                Main.data.cannonAmmo = tmp;

                tmp = Int32.Parse(inputRail.text);
                if (tmp < -1)
                    throw new ArgumentOutOfRangeException();
                Main.data.railgunAmmo = tmp;

                tmp = Int32.Parse(inputMissile.text);
                if (tmp < -1)
                    throw new ArgumentOutOfRangeException();
                Main.data.missileAmmo = tmp;

                tmp = Int32.Parse(inputDrone.text);
                if (tmp < -1)
                    throw new ArgumentOutOfRangeException();
                Main.data.droneParts = tmp;
                                
                pnlMain.SetActive(false);
            }
            catch
            {
                InfoPanelControl.inst.ShowWarning("Item values must be whole numbers 0 or larger (or -1 for auto sell).", 1, false);
            }
        }
    }
}
