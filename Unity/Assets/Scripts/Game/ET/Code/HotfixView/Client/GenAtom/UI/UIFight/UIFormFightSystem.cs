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
            self.OpenAllUIWidgets();
            // 创建本地战斗单位
            CreateLocalUnitsFromTables(self.Root()).Forget();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
        }

        private static async UniTask CreateLocalUnitsFromTables(Scene root)
        {
            var current = root.CurrentScene();
            var unis1 = new UniTask[1];

            var hidx = 0;
            {
                var unitInfo = UnitFactory.CreateHeroUniInfo(root);
                unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, hidx);
                unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
                Unit unit = UnitFactory.CreateFight(current, unitInfo);

                var t = EventSystem.Instance.PublishAsync(current, new AfterUnitCreate() { Unit = unit });
                unis1[hidx] = t;
                await UniTask.WhenAll(unis1);
            }

            var configs = Tables.Instance.DTMonster;
            var unis = new UniTask[configs.DataList.Count];
            for (int i = 0; i < configs.DataList.Count; i++)
            {
                var config = configs.DataList[i];
                UnitInfo unitInfo = UnitFactory.CreateUnitInfo(config,i);
                unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, i);
                unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
                Unit unit = UnitFactory.CreateFight(current, unitInfo);

                var t = EventSystem.Instance.PublishAsync(current, new AfterUnitCreate() { Unit = unit });
                unis[i] = t;
            }

            await UniTask.WhenAll(unis);
            SkillDiagFileLogger.MarkBattleLoadComplete("LocalFightUnits");
            current.TriggerGameAIChecks();
        }


        private static float3 GetLocalUnitPosition(UnitType unitType, int index)
        {
            return unitType switch
            {
                UnitType.Player => new float2(-2f, index * 1.5f).ToModePosition(),
                _ => new float2(2f, index * 1.5f).ToModePosition(),
            };
        }

        private static float3 GetLocalUnitForward(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Player => new float2(-1f, 0f).ToModeDirection(),
                UnitType.Monster => new float2(1f, 0f).ToModeDirection(),
                _ => float3.zero,
            };
        }
    }
}
