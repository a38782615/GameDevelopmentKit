using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(MapGenComponent))]
    [FriendOf(typeof(MapGenComponent))]
    public static partial class MapGenComponentSystem
    {
        private const int BattleVictorySkillExp = 100;

        [EntitySystem]
        private static void Awake(this MapGenComponent self)
        {
            self.VictoryRewardGranted = false;
        }

        [EntitySystem]
        private static void Destroy(this MapGenComponent self)
        {
        }

        public static int GetMap0(this MapGenComponent self)
        {
            return self.Root().GetPlayerData().Map0;
        }

        public static int GetMap1(this MapGenComponent self)
        {
            return self.Root().GetPlayerData().Map1;
        }

        public static DRStages GetStageConfig(this MapGenComponent self)
        {
            return GetStageConfig(self.GetMap0(), self.GetMap1());
        }

        public static async UniTask LoadBattleAsync(this MapGenComponent self)
        {
            if (self.GetStageConfig() == null)
            {
                return;
            }
            await self.CreateLocalUnitsFromTables();
        }

        public static async UniTask CreateLocalUnitsFromTables(this MapGenComponent self)
        {
            Scene root = self.Root();
            Scene current = self.GetParent<Scene>();
            self.VictoryRewardGranted = false;
            ClearLocalFightUnits(current);
            var unis1 = new UniTask[1];

            var hidx = 0;
            {
                var unitInfo = UnitFactory.CreateHeroUniInfo(root);
                unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, hidx);
                unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
                Unit unit = UnitFactory.CreateFight(current, unitInfo);

                var playerData = root.GetPlayerData();
                var attr = unit.GetComponent<AttributeComponent>();
                attr.SetValue(NumericType.Hp, playerData.Hp);

                var t = EventSystem.Instance.PublishAsync(current, new AfterUnitCreate() { Unit = unit });
                unis1[hidx] = t;
                await UniTask.WhenAll(unis1);
            }

            DRStages stageConfig = GetCurrentStageConfig(self);
            if (stageConfig == null || stageConfig.Monsters == null || stageConfig.Monsters.Length == 0)
            {
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
            current.TriggerGameAIChecks();
        }

        public static async UniTask TryGrantVictoryReward(this MapGenComponent self)
        {
            if (self == null || self.IsDisposed || self.VictoryRewardGranted || self.HasRemainingMonster())
            {
                return;
            }

            PlayerData playerData = self.Root().GetPlayerData();
            GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
            if (playerData == null || gameDataMgrComponent == null)
            {
                Log.Warning("Battle victory reward skipped because player data is missing.");
                return;
            }

            self.VictoryRewardGranted = true;
            playerData.SkillExp += BattleVictorySkillExp;
            await gameDataMgrComponent.SavePlayerData();
            Log.Info($"Battle victory rewarded SkillExp={BattleVictorySkillExp}, TotalSkillExp={playerData.SkillExp}.");
        }

        private static bool HasRemainingMonster(this MapGenComponent self)
        {
            UnitComponent unitComponent = self.GetParent<Scene>()?.GetComponent<UnitComponent>();
            if (unitComponent?.Children == null)
            {
                return false;
            }

            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is Unit unit && unit.Type() == UnitType.Monster)
                {
                    return true;
                }
            }

            return false;
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

        private static DRStages GetCurrentStageConfig(MapGenComponent self)
        {
            return GetStageConfig(self.GetMap0(), self.GetMap1());
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
