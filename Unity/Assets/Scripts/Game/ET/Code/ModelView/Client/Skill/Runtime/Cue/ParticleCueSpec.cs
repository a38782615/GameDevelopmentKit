namespace ET.Client
{
    /// <summary>
    /// 粒子特效 Cue Spec
    /// </summary>
    [ComponentOf(typeof(GameplayCueSpec))]
    public partial class ParticleCueSpec : Entity, IAwake
    {
        public int ParticleEntityId { get; set; }

        public PositionSourceType PositionSource { get; set; }

        public string ParticleBindingName { get; set; }

        public UnityEngine.Vector3 ParticleOffset { get; set; }

        public UnityEngine.Vector3 ParticleScale { get; set; }

        public bool AttachToTarget { get; set; }

        public bool ParticleLoop { get; set; }
    }
}
