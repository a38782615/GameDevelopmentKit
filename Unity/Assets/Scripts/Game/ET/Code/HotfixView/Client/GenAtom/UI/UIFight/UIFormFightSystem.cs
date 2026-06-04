using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    { 

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
        }

        private static async UniTask CreateLocalUnitsFromTables(Scene root)
        {
            var current = root.CurrentScene();
            var heros = Tables.Instance.DTHero;
            var unis1 = new UniTask[heros.DataList.Count];
            for (int i = 0; i < heros.DataList.Count; i++)
            {
                var config = heros.DataList[i];
                UnitInfo unitInfo = CreateUnitInfo(config, i);
                Unit unit = UnitFactory.Create(current, unitInfo);
                if (i == 0)
                {
                    root.GetComponent<PlayerComponent>().MyId = unitInfo.UnitId;
                }
                var t = EventSystem.Instance.PublishAsync(current, new AfterUnitCreate() { Unit = unit });
                unis1[i] = t;
            }
            await UniTask.WhenAll(unis1);

            var configs = Tables.Instance.DTMonster;
            var unis = new UniTask[configs.DataList.Count];
            for (int i = 0; i < configs.DataList.Count; i++)
            {
                var config = configs.DataList[i];
                UnitInfo unitInfo = CreateUnitInfo(config, i);
                Unit unit = UnitFactory.Create(current, unitInfo);

                var t = EventSystem.Instance.PublishAsync(current, new AfterUnitCreate() { Unit = unit });
                unis[i] = t;
            }

            await UniTask.WhenAll(unis);
        }

        private static UnitInfo CreateUnitInfo(DRHero config, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = config.Id;
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;

            unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, index);
            unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
            return unitInfo;
        }
        private static UnitInfo CreateUnitInfo(DRMonster config, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = config.Id;
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;
            unitInfo.PosIdx = index;

            unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, index);
            unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
            return unitInfo;
        }

        private static float3 GetLocalUnitPosition(UnitType unitType, int index)
        {
            return unitType switch
            {
                UnitType.Player => new float2(-3f + index * 2.5f, index * 1.5f).ToModePosition(),
                UnitType.Monster => new float2(3f + index * 2.5f, index * 1.5f).ToModePosition(),
                _ => new float2(index * 2.5f, -4f).ToModePosition(),
            };
        }

        private static float3 GetLocalUnitForward(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Player => new float2(1f, 0f).ToModeDirection(),
                UnitType.Monster => new float2(-1f, 0f).ToModeDirection(),
                _ => float3.zero,
            };
        }
    }
}
