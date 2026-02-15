// =============================================================================
// 此文件由 GameplayTagCodeGenerator 自动生成
// 请勿手动修改此文件，修改将在下次生成时被覆盖
// =============================================================================

using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 游戏标签静态库 - 提供所有标签的静态引用
    /// </summary>
    public static class GameplayTagLibrary
    {
        #region 标签定义

        /// <summary>
        /// Buff
        /// </summary>
        public static GameplayTag Buff { get; } = new GameplayTag("Buff");

        /// <summary>
        /// Buff.DeBuff
        /// </summary>
        public static GameplayTag Buff_DeBuff { get; } = new GameplayTag("Buff.DeBuff");

        /// <summary>
        /// Buff.DeBuff.Dot
        /// </summary>
        public static GameplayTag Buff_DeBuff_Dot { get; } = new GameplayTag("Buff.DeBuff.Dot");

        /// <summary>
        /// Buff.DeBuff.Stun
        /// </summary>
        public static GameplayTag Buff_DeBuff_Stun { get; } = new GameplayTag("Buff.DeBuff.Stun");

        /// <summary>
        /// CD
        /// </summary>
        public static GameplayTag CD { get; } = new GameplayTag("CD");

        /// <summary>
        /// CD.BeRecBlood
        /// </summary>
        public static GameplayTag CD_BeRecBlood { get; } = new GameplayTag("CD.BeRecBlood");

        /// <summary>
        /// CD.Blood
        /// </summary>
        public static GameplayTag CD_Blood { get; } = new GameplayTag("CD.Blood");

        /// <summary>
        /// CD.FireCircle
        /// </summary>
        public static GameplayTag CD_FireCircle { get; } = new GameplayTag("CD.FireCircle");

        /// <summary>
        /// CD.God
        /// </summary>
        public static GameplayTag CD_God { get; } = new GameplayTag("CD.God");

        /// <summary>
        /// CD.RecBlood
        /// </summary>
        public static GameplayTag CD_RecBlood { get; } = new GameplayTag("CD.RecBlood");

        /// <summary>
        /// CD.RuFood
        /// </summary>
        public static GameplayTag CD_RuFood { get; } = new GameplayTag("CD.RuFood");

        /// <summary>
        /// CD.SpeedUp
        /// </summary>
        public static GameplayTag CD_SpeedUp { get; } = new GameplayTag("CD.SpeedUp");

        /// <summary>
        /// CD.Sweep
        /// </summary>
        public static GameplayTag CD_Sweep { get; } = new GameplayTag("CD.Sweep");

        /// <summary>
        /// CD.ThreeFire
        /// </summary>
        public static GameplayTag CD_ThreeFire { get; } = new GameplayTag("CD.ThreeFire");

        /// <summary>
        /// CD.Wan
        /// </summary>
        public static GameplayTag CD_Wan { get; } = new GameplayTag("CD.Wan");

        /// <summary>
        /// CD.Wind
        /// </summary>
        public static GameplayTag CD_Wind { get; } = new GameplayTag("CD.Wind");

        /// <summary>
        /// Skill
        /// </summary>
        public static GameplayTag Skill { get; } = new GameplayTag("Skill");

        /// <summary>
        /// Skill.Running
        /// </summary>
        public static GameplayTag Skill_Running { get; } = new GameplayTag("Skill.Running");

        /// <summary>
        /// unitType
        /// </summary>
        public static GameplayTag unitType { get; } = new GameplayTag("unitType");

        /// <summary>
        /// unitType.hero
        /// </summary>
        public static GameplayTag unitType_hero { get; } = new GameplayTag("unitType.hero");

        /// <summary>
        /// unitType.monster
        /// </summary>
        public static GameplayTag unitType_monster { get; } = new GameplayTag("unitType.monster");

        #endregion

        #region 标签映射

        /// <summary>
        /// 标签名称到标签实例的映射
        /// </summary>
        [StaticField]
        public static readonly Dictionary<string, GameplayTag> TagMap = new Dictionary<string, GameplayTag>
        {
            ["Buff"] = Buff,
            ["Buff.DeBuff"] = Buff_DeBuff,
            ["Buff.DeBuff.Dot"] = Buff_DeBuff_Dot,
            ["Buff.DeBuff.Stun"] = Buff_DeBuff_Stun,
            ["CD"] = CD,
            ["CD.BeRecBlood"] = CD_BeRecBlood,
            ["CD.Blood"] = CD_Blood,
            ["CD.FireCircle"] = CD_FireCircle,
            ["CD.God"] = CD_God,
            ["CD.RecBlood"] = CD_RecBlood,
            ["CD.RuFood"] = CD_RuFood,
            ["CD.SpeedUp"] = CD_SpeedUp,
            ["CD.Sweep"] = CD_Sweep,
            ["CD.ThreeFire"] = CD_ThreeFire,
            ["CD.Wan"] = CD_Wan,
            ["CD.Wind"] = CD_Wind,
            ["Skill"] = Skill,
            ["Skill.Running"] = Skill_Running,
            ["unitType"] = unitType,
            ["unitType.hero"] = unitType_hero,
            ["unitType.monster"] = unitType_monster,
        };

        #endregion

        #region 辅助方法

        /// <summary>
        /// 根据名称获取标签
        /// </summary>
        public static GameplayTag GetTag(string tagName)
        {
            if (TagMap.TryGetValue(tagName, out var tag))
                return tag;
            return GameplayTag.None;
        }

        /// <summary>
        /// 检查标签是否存在
        /// </summary>
        public static bool HasTag(string tagName)
        {
            return TagMap.ContainsKey(tagName);
        }

        /// <summary>
        /// 获取所有标签名称
        /// </summary>
        public static IEnumerable<string> GetAllTagNames()
        {
            return TagMap.Keys;
        }

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public static IEnumerable<GameplayTag> GetAllTags()
        {
            return TagMap.Values;
        }

        #endregion
    }
}
