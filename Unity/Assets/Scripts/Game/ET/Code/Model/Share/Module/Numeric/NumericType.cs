using System;

namespace ET
{
	// 这个可弄个配置表生成
	public static class NumericType
	{
		public const int None = 0;
		public const int Max = 10000;

		public const int Speed = 1000;
		public const int SpeedBase = Speed * 10 + 1;
		public const int SpeedAdd = Speed * 10 + 2;
		public const int SpeedPct = Speed * 10 + 3;
		public const int SpeedFinalAdd = Speed * 10 + 4;
		public const int SpeedFinalPct = Speed * 10 + 5;

		// 生命值
		public const int Hp = 1001;
		public const int HpBase = Hp * 10 + 1;
		public const int HpAdd = Hp * 10 + 2;

		// 最大生命值
		public const int MaxHp = 1002;
		public const int MaxHpBase = MaxHp * 10 + 1;
		public const int MaxHpAdd = MaxHp * 10 + 2;
		public const int MaxHpPct = MaxHp * 10 + 3;
		public const int MaxHpFinalAdd = MaxHp * 10 + 4;
		public const int MaxHpFinalPct = MaxHp * 10 + 5;

		// 魔法值
		public const int Mp = 1003;
		public const int MpBase = Mp * 10 + 1;

		// 最大魔法值
		public const int MaxMp = 1004;
		public const int MaxMpBase = MaxMp * 10 + 1;
		public const int MaxMpAdd = MaxMp * 10 + 2;
		public const int MaxMpPct = MaxMp * 10 + 3;
		public const int MaxMpFinalAdd = MaxMp * 10 + 4;
		public const int MaxMpFinalPct = MaxMp * 10 + 5;

		// 吸血
		public const int SuckBlood = 1005;
		public const int SuckBloodBase = SuckBlood * 10 + 1;

		// 攻击力
		public const int Attack = 1006;
		public const int AttackBase = Attack * 10 + 1;
		public const int AttackAdd = Attack * 10 + 2;

		// 法强
		public const int MagicStrength = 1007;
		public const int MagicStrengthBase = MagicStrength * 10 + 1;
		public const int MagicStrengthAdd = MagicStrength * 10 + 2;

		// 护甲
		public const int Armor = 1008;
		public const int ArmorBase = Armor * 10 + 1;
		public const int ArmorAdd = Armor * 10 + 2;

		// 魔抗
		public const int MagicResistance = 1009;
		public const int MagicResistanceBase = MagicResistance * 10 + 1;
		public const int MagicResistanceAdd = MagicResistance * 10 + 2;

		// 护甲穿透
		public const int ArmorPenetration = 1010;
		public const int ArmorPenetrationBase = ArmorPenetration * 10 + 1;
		public const int ArmorPenetrationAdd = ArmorPenetration * 10 + 2;

		// 法术穿透
		public const int MagicPenetration = 1011;
		public const int MagicPenetrationBase = MagicPenetration * 10 + 1;
		public const int MagicPenetrationAdd = MagicPenetration * 10 + 2;

		// 暴击率
		public const int CriticalProbability = 1012;
		public const int CriticalProbabilityBase = CriticalProbability * 10 + 1;
		public const int CriticalProbabilityAdd = CriticalProbability * 10 + 2;

		// 技能冷却缩减
		public const int SkillCD = 1013;
		public const int SkillCDBase = SkillCD * 10 + 1;
		public const int SkillCDAdd = SkillCD * 10 + 2;

		// 生命恢复
		public const int HPRec = 1014;
		public const int HPRecBase = HPRec * 10 + 1;
		public const int HPRecAdd = HPRec * 10 + 2;

		// 魔法恢复
		public const int MPRec = 1015;
		public const int MPRecBase = MPRec * 10 + 1;
		public const int MPRecAdd = MPRec * 10 + 2;

		// 攻击速度
		public const int AttackSpeed = 1016;
		public const int AttackSpeedBase = AttackSpeed * 10 + 1;
		public const int AttackSpeedAdd = AttackSpeed * 10 + 2;

		// 攻速收益
		public const int AttackSpeedIncome = 1017;
		public const int AttackSpeedIncomeBase = AttackSpeedIncome * 10 + 1;
		public const int AttackSpeedIncomeAdd = AttackSpeedIncome * 10 + 2;

		// 等级
		public const int Level = 1018;
		public const int LevelBase = Level * 10 + 1;

		// 最大等级
		public const int MaxLevel = 1019;
		public const int MaxLevelBase = MaxLevel * 10 + 1;

		// 暴击伤害
		public const int CriticalStrikeHarm = 1020;
		public const int CriticalStrikeHarmBase = CriticalStrikeHarm * 10 + 1;

		// 攻击距离
		public const int AttackRange = 1021;
		public const int AttackRangeBase = AttackRange * 10 + 1;

		// 心情值
		public const int Mode = 1022;
		public const int ModeBase = Mode * 10 + 1;

		public const int ModeMax = 1023;
		public const int ModeMaxBase = ModeMax * 10 + 1;
		public const int ModeMaxAdd = ModeMax * 10 + 2;
		public const int ModeMaxPct = ModeMax * 10 + 3;
		public const int ModeMaxFinalAdd = ModeMax * 10 + 4;
		public const int ModeMaxFinalPct = ModeMax * 10 + 5;

		public const int Damage = 1024;
		public const int DamageBase = Damage * 10 + 1;
		public const int DamageAdd = Damage * 10 + 2;
		public const int DamagePct = Damage * 10 + 3;

		public const int Experience = 1025;
		public const int ExperienceBase = Experience * 10 + 1;

		public const int IncomingDamage = 1026;
		public const int IncomingDamageBase = IncomingDamage * 10 + 1;

		public const int IncomingHeal = 1027;
		public const int IncomingHealBase = IncomingHeal * 10 + 1;

		public const int AOI = 3003;
		public const int AOIBase = AOI * 10 + 1;
		public const int AOIAdd = AOI * 10 + 2;
		public const int AOIPct = AOI * 10 + 3;
		public const int AOIFinalAdd = AOI * 10 + 4;
		public const int AOIFinalPct = AOI * 10 + 5;

		public static int[] GetClientAttributeTypes()
		{
			return new[]
			{
				Hp,
				MaxHp,
				HPRec,
				Mp,
				MaxMp,
				MPRec,
				Attack,
				Armor,
				MagicStrength,
				MagicResistance,
				Speed,
				AttackSpeed,
				SkillCD,
				CriticalProbability,
				CriticalStrikeHarm,
				Level,
				Experience,
				IncomingDamage,
				IncomingHeal,
			};
		}

		public static int GetBaseNumericType(int numericType)
		{
			switch (numericType)
			{
				case Speed:
					return SpeedBase;
				case Hp:
					return HpBase;
				case MaxHp:
					return MaxHpBase;
				case Mp:
					return MpBase;
				case MaxMp:
					return MaxMpBase;
				case Attack:
					return AttackBase;
				case MagicStrength:
					return MagicStrengthBase;
				case Armor:
					return ArmorBase;
				case MagicResistance:
					return MagicResistanceBase;
				case CriticalProbability:
					return CriticalProbabilityBase;
				case SkillCD:
					return SkillCDBase;
				case HPRec:
					return HPRecBase;
				case MPRec:
					return MPRecBase;
				case AttackSpeed:
					return AttackSpeedBase;
				case Level:
					return LevelBase;
				case CriticalStrikeHarm:
					return CriticalStrikeHarmBase;
				case Mode:
					return ModeBase;
				case ModeMax:
					return ModeMaxBase;
				case Damage:
					return DamageBase;
				case Experience:
					return ExperienceBase;
				case IncomingDamage:
					return IncomingDamageBase;
				case IncomingHeal:
					return IncomingHealBase;
				default:
					return None;
			}
		}

		public static string GetAttributeName(int numericType)
		{
			switch (numericType)
			{
				case Hp: return "Health";
				case MaxHp: return "Max Health";
				case HPRec: return "Health Regen";
				case Mp: return "Mana";
				case MaxMp: return "Max Mana";
				case MPRec: return "Mana Regen";
				case Attack: return "Attack";
				case Armor: return "Defense";
				case MagicStrength: return "Magic Power";
				case MagicResistance: return "Magic Defense";
				case Speed: return "Move Speed";
				case AttackSpeed: return "Attack Speed";
				case SkillCD: return "Cooldown Reduction";
				case CriticalProbability: return "Crit Rate";
				case CriticalStrikeHarm: return "Crit Damage";
				case Level: return "Level";
				case Experience: return "Experience";
				case IncomingDamage: return "Incoming Damage";
				case IncomingHeal: return "Incoming Heal";
				default: return numericType.ToString();
			}
		}

		public static bool TryParseAttributeName(string name, out int numericType)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				numericType = None;
				return false;
			}

			string normalized = name.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
			switch (normalized)
			{
				case "none":
					numericType = None;
					return true;
				case "hp":
				case "health":
					numericType = Hp;
					return true;
				case "maxhp":
				case "maxhealth":
					numericType = MaxHp;
					return true;
				case "hprec":
				case "healthregen":
					numericType = HPRec;
					return true;
				case "mp":
				case "mana":
					numericType = Mp;
					return true;
				case "maxmp":
				case "maxmana":
					numericType = MaxMp;
					return true;
				case "mprec":
				case "manaregen":
					numericType = MPRec;
					return true;
				case "attack":
					numericType = Attack;
					return true;
				case "armor":
				case "defense":
					numericType = Armor;
					return true;
				case "magicstrength":
				case "magicpower":
					numericType = MagicStrength;
					return true;
				case "magicresistance":
				case "magicdefense":
					numericType = MagicResistance;
					return true;
				case "speed":
				case "movespeed":
					numericType = Speed;
					return true;
				case "attackspeed":
					numericType = AttackSpeed;
					return true;
				case "skillcd":
				case "cooldownreduction":
					numericType = SkillCD;
					return true;
				case "criticalprobability":
				case "critrate":
					numericType = CriticalProbability;
					return true;
				case "criticalstrikeharm":
				case "critdamage":
					numericType = CriticalStrikeHarm;
					return true;
				case "level":
					numericType = Level;
					return true;
				case "experience":
					numericType = Experience;
					return true;
				case "incomingdamage":
					numericType = IncomingDamage;
					return true;
				case "incomingheal":
					numericType = IncomingHeal;
					return true;
				default:
					numericType = None;
					return false;
			}
		}
	}
}
