using System;

namespace ET.Client
{
    /// <summary>
    /// 正在播放的 Cue 实例。
    /// </summary>
    [EnableClass]
    public class ActiveGameplayCue
    {
        public bool IsExpired { get; private set; }

        public float StartTime { get; private set; }

        public float Duration { get; set; }

        public float ElapsedTime { get; private set; }

        public bool IsLooping { get; set; }

        public UnityEngine.GameObject AttachedObject { get; set; }

        public EntityRef<UGFEntity> AttachedEffectEntity { get; set; }

        public UnityEngine.AudioSource AttachedAudioSource { get; set; }

        public bool WasRefreshed { get; set; }

        public event Action OnRemoved;

        public ActiveGameplayCue()
        {
            IsExpired = false;
            StartTime = UnityEngine.Time.time;
            ElapsedTime = 0f;
            Duration = 0f;
            IsLooping = false;
            WasRefreshed = false;
        }

        public void Tick(float deltaTime)
        {
            if (IsExpired)
            {
                return;
            }

            ElapsedTime += deltaTime;
            if (Duration > 0f && ElapsedTime >= Duration)
            {
                if (IsLooping)
                {
                    ElapsedTime = 0f;
                    return;
                }

                Expire();
            }
        }

        public void Expire()
        {
            if (IsExpired)
            {
                return;
            }

            IsExpired = true;
        }

        public void Stop()
        {
            Expire();

            UGFEntity effectEntity = AttachedEffectEntity.As();
            if (effectEntity != null)
            {
                effectEntity.Dispose();
                AttachedEffectEntity = default;
                AttachedObject = null;
            }
            else if (AttachedObject != null)
            {
                UnityEngine.Object.Destroy(AttachedObject);
                AttachedObject = null;
            }

            if (AttachedAudioSource != null)
            {
                AttachedAudioSource.Stop();
                AttachedAudioSource = null;
            }

            OnRemoved?.Invoke();
        }

        public float GetRemainingTime()
        {
            if (Duration <= 0f)
            {
                return float.MaxValue;
            }

            return UnityEngine.Mathf.Max(0f, Duration - ElapsedTime);
        }

        public override string ToString()
        {
            return $"[ActiveCue] Elapsed={ElapsedTime:F2}s, Expired={IsExpired}";
        }
    }
}
