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
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Chaos;
using System.Collections.Generic;
using UnityEngine;
//Reference: 0Harmony

namespace Oxide.Plugins
{
    [Info("TinCanBoom", "RFC1920", "1.0.1")]
    [Description("Add explosives to TinCanAlarm")]
    internal class TinCanBoom : RustPlugin
    {
        ConfigData configData;
        private Dictionary<ulong, List<TinCanEnhanced>> playerAlarms = new Dictionary<ulong, List<TinCanEnhanced>>();
        public const string permUse = "tincanboom.use";

        private readonly List<string> orDefault = new List<string>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        private void LMessage(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        public class TinCanEnhanced
        {
            public string location;
            public NetworkableId alarm;
            public NetworkableId te;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "off", "OFF" },
                { "on", "ON" },
                { "notauthorized", "You don't have permission to do that !!" },
                { "enabled", "TinCanBoom enabled" },
                { "disabled", "TinCanBoom disabled" }
            }, this);
        }

        [Command("ente"), Permission(permUse)]
        private void EnableDisable(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.HasPermission(permUse) && configData.Options.RequirePermission) { Message(iplayer, "notauthorized"); return; }

            bool en = configData.Options.startEnabled;
            if (orDefault.Contains(iplayer.Id))
            {
                orDefault.Remove(iplayer.Id);
            }
            else
            {
                orDefault.Add(iplayer.Id);
                en = !en;
            }
            switch (en)
            {
                case true:
                    Message(iplayer, "enabled");
                    break;
                case false:
                    Message(iplayer, "disabled");
                    break;
            }
        }

        private void OnServerInitialized()
        {
            LoadConfigVariables();
            AddCovalenceCommand("ente", "EnableDisable");
            permission.RegisterPermission(permUse, this);
            LoadData();
        }

        private void OnEntitySpawned(TinCanAlarm alarm)
        {
            BasePlayer player = FindPlayerByID(alarm.OwnerID);
            if (player == null) return;
            if (configData.Options.RequirePermission && !player.HasPermission(permUse)) return;

            if (configData.Options.startEnabled && orDefault.Contains(player.UserIDString))
            {
                DoLog("Plugin enabled by default, but player-disabled");
                return;
            }
            else if (!configData.Options.startEnabled && !orDefault.Contains(player.UserIDString))
            {
                DoLog("Plugin disabled by default, and not player-enabled");
                return;
            }

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
            if (configData.Options.debug) Puts(message);
        }

        [AutoPatch]
        [HarmonyPatch(typeof(TinCanAlarm), "TriggerAlarm")]
        public static class TriggerAlarmPatch
        {
            [HarmonyPrefix]
            static void Prefix(TinCanAlarm __instance)
            {
                Interface.CallHook("OnTinCanAlarmTrigger", __instance);
            }
        }

        private void OnTinCanAlarmTrigger(TinCanAlarm alarm)
        {
            if (alarm == null) return;
            RFTimedExplosive te = alarm.gameObject.GetComponentInChildren<RFTimedExplosive>();
            if (te != null)
            {
                te._limitedNetworking = false;
                te.SetFuse(configData.Options.fireDelay);
                te.SetFlag(BaseEntity.Flags.On, true, false, false);

                BasePlayer player = FindPlayerByID(alarm.OwnerID);
                DoLog($"Removing destroyed alarm from data for {player?.displayName}");

                playerAlarms[player.userID].RemoveAll(x => x.te == te.net.ID);
                SaveData();
            }
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

        #region config
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Options = new Options()
                {
                    RequirePermission = true,
                    startEnabled = false,
                    fireDelay = 2f,
                    debug = false
                },
                Version = Version
            };

            SaveConfig(config);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        private class ConfigData
        {
            public Options Options;
            public VersionNumber Version;
        }

        public class Options
        {
            public bool RequirePermission;
            public bool startEnabled;
            public float fireDelay;
            public bool debug;
        }
        #endregion
    }
}
