using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(FightComponent))]
    [FriendOf(typeof(FightComponent))]
    public static partial class FightComponentSystem
    {
        [EntitySystem]
        private static void Awake(this FightComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this FightComponent self)
        {
        }

        public static DRStages GetStageConfig(this FightComponent self, int subLevel)
        {
            return GetStageConfig(self.CurrentMap, subLevel);
        }

        public static async UniTask LoadBattleAsync(this FightComponent self, int subLevel)
        {
            if (self.GetStageConfig(subLevel) == null)
            {
                return;
            }

            self.CurrentLevel = subLevel;
            await self.CreateLocalUnitsFromTables();
        }

        public static async UniTask CreateLocalUnitsFromTables(this FightComponent self)
        {
            Scene root = self.Root();
            Scene current = self.GetParent<Scene>();
            ClearLocalFightUnits(current);
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

            DRStages stageConfig = GetCurrentStageConfig(self);
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

        private static void ClearLocalFightUnits(Scene current)
        {
            UnitComponent unitComponent = current?.GetComponent<UnitComponent>();
            if (unitComponent?.Children == null)
            {
                return;
            }

            using ListComponent<long> unitIds = ListComponent<long>.Create();
            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is Unit unit)
                {
                    unitIds.Add(unit.Id);
                }
            }

            foreach (long unitId in unitIds)
            {
                unitComponent.Remove(unitId);
            }
        }

        private static DRStages GetCurrentStageConfig(FightComponent self)
        {
            return GetStageConfig(self.CurrentMap, self.CurrentLevel);
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
