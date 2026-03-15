using System;

namespace ET
{

    public class DataModifier : Object, IPool, IDisposable, IEquatable<DataModifier>
    {
        public int Id;
        /// <summary>
        /// 修改器类型
        /// </summary>
        public int Attribute;
        private float v;
        /// <summary>
        /// 修改的值
        /// </summary>
        public float Value
        {
            get
            {
                return v;

            }
            set
            {
                v = value;
            }
        }

        public bool IsFromPool { get; set; }

        public bool Equals(DataModifier other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Attribute == other.Attribute && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DataModifier)obj);
        }

        public override int GetHashCode()
        {
            return (int)Id;
        }

        public static DataModifier Create(int id, int attr, float v)
        {
            var modifier = ObjectPool.Instance.Fetch<DataModifier>();
            modifier.Id = id;
            modifier.Attribute = attr;
            modifier.Value = v;
            return modifier;
        }

        public void Dispose()
        {
            this.Value = 0;
            ObjectPool.Instance.Recycle(this);
        }
    }
}