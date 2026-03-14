using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class Move2DComponent: Entity, IAwake, IDestroy
    {
        public float2 PreTarget
        {
            get
            {
                return this.Targets[this.N - 1];
            }
        }

        public float2 NextTarget
        {
            get
            {
                return this.Targets[this.N];
            }
        }

        public long BeginTime;

        public long StartTime { get; set; }

        public float2 StartPos;

        public float2 RealPos
        {
            get
            {
                return this.Targets[0];
            }
        }

        private long needTime;

        public long NeedTime
        {
            get
            {
                return this.needTime;
            }
            set
            {
                this.needTime = value;
            }
        }

        public long MoveTimer;

        public float Speed;

        public AutoResetUniTaskCompletionSource<bool> tcs;

        public List<float2> Targets = new List<float2>();

        public float2 FinalTarget
        {
            get
            {
                return this.Targets[this.Targets.Count - 1];
            }
        }

        public int N;

        public int TurnTime;

        public quaternion From;

        public quaternion To;
    }
}
