using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        private const int InitialLevel = 0;
        private const int InitialSubLevel = 0;

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

            DRStages stageConfig = GetCurrentStageConfig(root);
            if (stageConfig == null || stageConfig.Monsters == null || stageConfig.Monsters.Length == 0)
            {
                SkillDiagFileLogger.MarkBattleLoadComplete("LocalFightUnits");
                current.TriggerGameAIChecks();
                return;
            }

            var unis = new UniTask[stageConfig.Monsters.Length];
            for (int i = 0; i < stageConfig.Monsters.Length; i++)
            {
                DRMonster config = Tables.Instance.DTMonster.GetOrDefault(stageConfig.Monsters[i]);
                if (config == null)
                {
                    unis[i] = UniTask.CompletedTask;
                    continue;
                }

                UnitInfo unitInfo = UnitFactory.CreateUnitInfo(config, i);
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

        private static DRStages GetCurrentStageConfig(Scene root)
        {
            int level = InitialLevel;
            int subLevel = InitialSubLevel;
            PlayerData playerData = root.GetComponent<GameDataMgrComponent>()?.GetPlayerDataComponent()?.PlayerData;
            if (playerData != null)
            {
                level = playerData.Level;
                subLevel = playerData.SubLevel;
            }

            return GetStageConfig(level, subLevel);
        }

        private static DRStages GetStageConfig(int level, int subLevel)
        {
            foreach (DRStages stageConfig in Tables.Instance.DTStages.DataList)
            {
                if (stageConfig.Level == level && stageConfig.SubLevel == subLevel)
                {
                    return stageConfig;
                }
            }

            return null;
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
