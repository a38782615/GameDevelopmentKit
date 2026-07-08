

namespace ET.Client
{
    public partial class PlayerData : Object
    {
        public long Id;
        public string NickName; //昵称
        public int ConfigId;
        public int SubLevel;//子等级
        public int Level; //等级
        public int Age; //年龄
        public int Exp; //经验
        public long Hp; //当前血
        public int Diamond; //灵石
        public ET.XRoot XRoot; // 灵根
        public int ElixirPoison; // 丹毒
        public int Physique; // 体魄
        public int Comprehension; // 悟性
        public int DivineSense; // 神识
        public int Fortune; // 福缘
        public int PosIdx; //位置
        public int Map0;
        public int Map1;
    }
}
