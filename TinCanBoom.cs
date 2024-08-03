using HarmonyLib;
using Oxide.Core.Plugins;
using UnityEngine;
//Reference: 0Harmony

namespace Oxide.Plugins
{
    [Info("TinCanBoom", "RFC1920", "0.0.1")]
    [Description("")]
    internal class TinCanBoom : RustPlugin
    {
        private bool debug = true;
        private const string expPrefab = "assets/prefabs/resource/explosives/explosives.item.prefab";
        private void OnEntitySpawned(TinCanAlarm alarm)
        {
            RFTimedExplosive exp = GameManager.server.CreateEntity("assets/prefabs/tools/c4/explosive.timed.deployed.prefab") as RFTimedExplosive;
            exp.enableSaving = false;
            exp.CancelInvokeFixedTime(null);
            exp.flags = 0;
            exp.timerAmountMin = float.PositiveInfinity;
            exp.timerAmountMax = float.PositiveInfinity;
            exp.triggers = alarm.triggers;
            exp.SetFuse(float.PositiveInfinity);
            exp.transform.localPosition = new Vector3(0f, 1f, 0f);
            exp.SetParent(alarm);
            RemoveComps(exp);
            exp.Spawn();
            exp.stickEffect = null;
            UnityEngine.Object.DestroyImmediate(exp.beepLoop);
            exp.UpdateNetworkGroup();

            //BaseEntity exp = GameManager.server.CreateEntity(expPrefab, alarm.transform.position, alarm.transform.rotation, true);
            //TimedExplosive te = exp as TimedExplosive;
            //te.timerAmountMax = 0;
            //te.triggers = alarm.triggers;
            //exp.Spawn();
            SpawnRefresh(exp);
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
                    te.SetFuse(0);
                    te.SetFlag(BaseEntity.Flags.On, true, false, false);
                }
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
    }
}
