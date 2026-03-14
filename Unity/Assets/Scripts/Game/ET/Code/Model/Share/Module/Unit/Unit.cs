using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;
using Unity.Mathematics;

namespace ET
{
    [ChildOf(typeof(UnitComponent))]
    [DebuggerDisplay("ViewName,nq")]
    public partial class Unit: Entity, IAwake<int>
    {
        public int ConfigId { get; set; } //配置表id

        [BsonElement]
        private float3 position; //坐标

        [BsonIgnore]
        public float3 Position
        {
            get => this.position;
            set
            {
                float3 oldPos = this.position;
                this.position = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangePosition() { Unit = this, OldPos = oldPos });
            }
        }

        [BsonIgnore]
        public float3 Forward
        {
            get
            {
                if (global::ET.ModeDefine.Is2D)
                {
                    return math.mul(this.Rotation, new float3(1f, 0f, 0f));
                }

                return math.mul(this.Rotation, math.forward());
            }
            set
            {
                if (global::ET.ModeDefine.Is2D)
                {
                    float2 planar = new float2(value.x, value.y);
                    if (math.lengthsq(planar) < 0.0001f)
                    {
                        this.Rotation = quaternion.identity;
                        return;
                    }

                    this.Rotation = quaternion.RotateZ(math.atan2(planar.y, planar.x));
                    return;
                }

                this.Rotation = quaternion.LookRotation(value, math.up());
            }
        }
        
        [BsonElement]
        private quaternion rotation;
        
        [BsonIgnore]
        public quaternion Rotation
        {
            get => this.rotation;
            set
            {
                this.rotation = value;
                EventSystem.Instance.Publish(this.Scene(), new ChangeRotation() { Unit = this });
            }
        }

        protected override string ViewName
        {
            get
            {
                return $"{this.GetType().FullName} ({this.Id})";
            }
        }
    }
}
