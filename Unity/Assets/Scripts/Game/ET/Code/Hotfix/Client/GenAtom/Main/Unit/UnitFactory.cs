using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public static partial class UnitFactory
    {
        public static Unit Create(Scene currentScene, UnitInfo unitInfo)
        {
            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            Unit unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.UnitId, unitInfo.ConfigId);
            unitComponent.Add(unit);
            unit.PosIdx = unitInfo.PosIdx;
            unit.Position = unitInfo.Position;
            unit.Forward = unitInfo.Forward;
            unit.AddMoveComponentByMode();

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();

            foreach (var kv in unitInfo.KV)
            {
                numericComponent.Set(kv.Key, kv.Value);
            }
            unit.GetOrAddComponent<global::ET.AttributeComponent>();
            unit.GetOrAddComponent<MovementAgent>();
            if (unitInfo.MoveInfo != null)
            {
                if (unitInfo.MoveInfo.Points.Count > 0)
                {
                    unitInfo.MoveInfo.Points[0] = unit.Position;
                    unit.MoveToAsync(unitInfo.MoveInfo.Points).Forget();
                }
            }

            unit.AddComponent<ObjectWait>();

            if (Tables.Instance.DTGameAI.GameAIs.ContainsKey(unit.ConfigId)
                && unit.GetComponent<GameAIComponent>() == null)
            {
                unit.AddComponent<GameAIComponent, int>(unit.ConfigId);
            }

            // unit.AddComponent<XunLuoPathComponent>();

            // EventSystem.Instance.Publish(unit.Scene(), new AfterUnitCreate() { Unit = unit });
            return unit;
        }
    }
}
