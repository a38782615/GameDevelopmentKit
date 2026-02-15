using System.Collections.Generic;

namespace ET.Client
{
    public class UnitManager : Singleton<UnitManager>
    {
        private readonly List<SkillUnit> _units = new List<SkillUnit>();

        public IReadOnlyList<SkillUnit> Units => _units;

        public void Register(SkillUnit unit)
        {
            if (!_units.Contains(unit))
                _units.Add(unit);
        }

        public void Unregister(SkillUnit unit)
        {
            _units.Remove(unit);
        }

        public T GetUnit<T>() where T : SkillUnit
        {
            foreach (var unit in _units)
            {
                if (unit is T t)
                    return t;
            }
            return null;
        }

        public List<T> GetUnits<T>() where T : SkillUnit
        {
            var result = new List<T>();
            foreach (var unit in _units)
            {
                if (unit is T t)
                    result.Add(t);
            }
            return result;
        }
    }
}