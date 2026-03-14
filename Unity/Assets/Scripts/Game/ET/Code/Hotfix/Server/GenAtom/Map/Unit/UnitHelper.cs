using System.Collections.Generic;

namespace ET.Server
{
    [FriendOf(typeof(NumericComponent))]
    public static partial class UnitHelper
    {
        public static UnitInfo CreateUnitInfo(Unit unit)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            NumericComponent nc = unit.GetComponent<NumericComponent>();
            unitInfo.UnitId = unit.Id;
            unitInfo.ConfigId = unit.ConfigId;
            unitInfo.Type = (int)unit.Type();
            unitInfo.Position = unit.Position;
            unitInfo.Forward = unit.Forward;

            if (unit.TryCreateMoveInfo(out MoveInfo moveInfo))
            {
                unitInfo.MoveInfo = moveInfo;
            }

            foreach ((int key, long value) in nc.NumericDic)
            {
                unitInfo.KV.Add(key, value);
            }

            return unitInfo;
        }
        
        // 获取看见unit的玩家，主要用于广播
        public static Dictionary<long, EntityRef<AOIEntity>> GetBeSeePlayers(this Unit self)
        {
            return self.GetComponent<AOIEntity>().GetBeSeePlayers();
        }
    }
}
