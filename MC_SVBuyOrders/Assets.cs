using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MC_SVBuyOrders
{
    internal class Assets
    {
        internal static GameObject pnlMain;

        internal static void LoadAssets(string path)
        {
            AssetBundle assets = AssetBundle.LoadFromFile(path);
            GameObject pack = assets.LoadAsset<GameObject>("Assets/mc_svbuyorders.prefab");

            pnlMain = pack.transform.Find("mc_svbuyorderPanel").gameObject;
        }
    }
}
