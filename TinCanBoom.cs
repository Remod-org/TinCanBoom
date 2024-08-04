#region License (GPL v2)
/*
    TinCanBoom! Add Explosive to TinCanAlarm
    Copyright (c) 2024 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; version 2
    of the License only.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License (GPL v2)
using HarmonyLib;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;
//Reference: 0Harmony

namespace Oxide.Plugins
{
    [Info("TinCanBoom", "RFC1920", "0.0.3")]
    [Description("Add explosives to TinCanAlarm")]
    internal class TinCanBoom : RustPlugin
    {
        private bool debug = true;
        private Dictionary<ulong, List<TinCanEnhanced>> playerAlarms = new Dictionary<ulong, List<TinCanEnhanced>>();
        public class TinCanEnhanced
        {
            public string location;
            public NetworkableId alarm;
            public NetworkableId te;
        }

        private void OnServerInitialized()
        {
            LoadData();
        }

        private void OnEntitySpawned(TinCanAlarm alarm)
        {
            BasePlayer player = FindPlayerByID(alarm.OwnerID);
            if (player == null) return;
            RFTimedExplosive exp = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab") as RFTimedExplosive;
            exp.enableSaving = false;
            exp.transform.localPosition = new Vector3(0f, 1f, 0f);
            exp.flags = 0;
            exp.SetFuse(float.PositiveInfinity);
            exp.timerAmountMin = float.PositiveInfinity;
            exp.timerAmountMax = float.PositiveInfinity;

            exp.SetParent(alarm);
            RemoveComps(exp);
            exp.Spawn();
            exp.stickEffect = null;
            UnityEngine.Object.DestroyImmediate(exp.beepLoop);
            exp.beepLoop = null;
            exp._limitedNetworking = true;
            exp.SendNetworkUpdateImmediate();

            SpawnRefresh(exp);
            if (!playerAlarms.ContainsKey(player.userID))
            {
                playerAlarms.Add(player.userID, new List<TinCanEnhanced>());
            }
            playerAlarms[player.userID].Add(new TinCanEnhanced()
            {
                location = alarm.transform.position.ToString(),
                alarm = alarm.net.ID,
                te = exp.net.ID
            });
            SaveData();
        }

        private void DoLog(string message)
        {
            if (debug) Puts(message);
        }

        [AutoPatch]
        [HarmonyPatch(typeof(TinCanAlarm), "TriggerAlarm")]
        public static class TriggerAlarmPatch
        {
            [HarmonyPrefix]
            static void Prefix(TinCanAlarm __instance)
            {
                RFTimedExplosive te = __instance.gameObject.GetComponentInChildren<RFTimedExplosive>();
                if (te != null)
                {
                    Interface.CallHook("OnTinCanAlarmTrigger", __instance, te);
                }
            }
        }

        private void OnTinCanAlarmTrigger(TinCanAlarm entity, RFTimedExplosive te)
        {
            te?.SetFuse(0);
            te?.SetFlag(BaseEntity.Flags.On, true, false, false);

            BasePlayer player = FindPlayerByID(entity.OwnerID);
            DoLog($"Removing destroyed alarm from data for {player?.displayName}");

            playerAlarms[player.userID].RemoveAll(x => x.te == te.net.ID);
            SaveData();
        }

        public void RemoveComps(BaseEntity obj)
        {
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<GroundWatch>());
            foreach (MeshCollider mesh in obj.GetComponentsInChildren<MeshCollider>())
            {
                DoLog($"Destroying MeshCollider for {obj.ShortPrefabName}");
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private BasePlayer FindPlayerByID(ulong userid, bool includeSleepers = true)
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID == userid)
                {
                    return activePlayer;
                }
            }
            if (includeSleepers)
            {
                foreach (BasePlayer sleepingPlayer in BasePlayer.sleepingPlayerList)
                {
                    if (sleepingPlayer.userID == userid)
                    {
                        return sleepingPlayer;
                    }
                }
            }
            return null;
        }

        private void SpawnRefresh(BaseEntity entity)
        {
            StabilityEntity hasstab = entity.GetComponent<StabilityEntity>();
            if (hasstab != null)
            {
                hasstab.grounded = true;
            }
            BaseMountable hasmount = entity.GetComponent<BaseMountable>();
            if (hasmount != null)
            {
                hasmount.isMobile = true;
            }
        }

        #region Data
        private void LoadData()
        {
            playerAlarms = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, List<TinCanEnhanced>>>(Name + "/playerAlarms");
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name + "/playerAlarms", playerAlarms);
        }
        #endregion Data
    }
}
