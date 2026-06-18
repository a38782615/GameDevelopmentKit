using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public static partial class UnitFactory
    {
        public static UnitInfo CreateHeroUniInfo(Scene root)
        {
            PlayerData playerData = root?.GetComponent<GameDataMgrComponent>()?.GetPlayerDataComponent()?.PlayerData;
            var heroConfig = Tables.Instance.DTHero.Get(playerData.Id);
            UnitInfo unitInfo = UnitFactory.CreateUnitInfo(heroConfig, playerData.Level, playerData.SubLevel, playerData.PosIdx);
            return unitInfo;
        }

        public static Unit CreateFight(Scene currentScene, UnitInfo unitInfo, bool needMove = false)
        {
            var unit = CreateData(currentScene, unitInfo);
            if (needMove)
            {
                unit.AddMoveComponentByMode();
                if (unitInfo.MoveInfo != null)
                {
                    if (unitInfo.MoveInfo.Points.Count > 0)
                    {
                        unitInfo.MoveInfo.Points[0] = unit.Position;
                        unit.MoveToAsync(unitInfo.MoveInfo.Points).Forget();
                    }
                }
            }

            unit.AddComponent<ObjectWait>();

            if (Tables.Instance.DTGameAI.GameAIs.ContainsKey(unit.ConfigId))
            {
                unit.GetOrAddComponent<GameAIComponent, int>(unit.ConfigId);
            }

            // unit.AddComponent<XunLuoPathComponent>();

            // EventSystem.Instance.Publish(unit.Scene(), new AfterUnitCreate() { Unit = unit });
            return unit;
        }

        public static Unit CreateData(Scene currentScene, UnitInfo unitInfo)
        {
            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            Unit unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.UnitId, unitInfo.ConfigId);
            unit.PosIdx = unitInfo.PosIdx;
            unit.Position = unitInfo.Position;
            unit.Forward = unitInfo.Forward;
            unit.Level = unitInfo.Level;
            unit.SubLevel = unitInfo.SubLevel;

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            foreach (var kv in unitInfo.KV)
            {
                numericComponent.Set(kv.Key, kv.Value);
            }
            unit.GetOrAddComponent<global::ET.AttributeComponent, int, int, int>(unit.ConfigId, unit.Level, unit.SubLevel);
            return unit;
        }


        public static UnitInfo CreateUnitInfo(DRHero config, int level, int subLevel, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = config.Id;
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;
            unitInfo.PosIdx = index;

            unitInfo.Level = level;
            unitInfo.SubLevel = subLevel;
            return unitInfo;
        }

        public static UnitInfo CreateUnitInfo(DRMonster config, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = config.Id;
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;
            unitInfo.PosIdx = index;

            unitInfo.Level = config.Level;
            unitInfo.SubLevel = config.SubLevel;
            return unitInfo;
        }

    }
}
