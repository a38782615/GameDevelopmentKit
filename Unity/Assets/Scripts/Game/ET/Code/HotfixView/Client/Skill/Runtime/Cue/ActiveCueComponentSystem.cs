using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(ActiveCueComponent))]
    [EntitySystemOf(typeof(ActiveCueComponent))]
    public static partial class ActiveCueComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ActiveCueComponent self)
        {
            self.ResetForPlay(false);
        }

        [EntitySystem]
        private static void Destroy(this ActiveCueComponent self)
        {
            self.ReleaseRuntimeObjects();
        }

        public static void ResetForPlay(this ActiveCueComponent self, bool isLooping)
        {
            if (self == null)
            {
                return;
            }

            self.ReleaseRuntimeObjects();
            self.IsExpired = false;
            self.StartTime = Time.time;
            self.Duration = 0f;
            self.ElapsedTime = 0f;
            self.IsLooping = isLooping;
            self.FloatingTextHandle = 0;
        }

        public static void Tick(this ActiveCueComponent self, float deltaTime)
        {
            if (self == null || self.IsExpired)
            {
                return;
            }

            self.ElapsedTime += deltaTime;
            if (self.Duration <= 0f || self.ElapsedTime < self.Duration)
            {
                return;
            }

            if (self.IsLooping)
            {
                self.ElapsedTime = 0f;
                return;
            }

            self.Expire();
        }

        public static void Expire(this ActiveCueComponent self)
        {
            if (self == null || self.IsExpired)
            {
                return;
            }

            self.IsExpired = true;
        }

        public static void Stop(this ActiveCueComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.IsExpired = true;
            self.ReleaseRuntimeObjects();
        }

        public static float GetRemainingTime(this ActiveCueComponent self)
        {
            if (self == null || self.Duration <= 0f)
            {
                return float.MaxValue;
            }

            return Mathf.Max(0f, self.Duration - self.ElapsedTime);
        }

        public static void PlaySound(this ActiveCueComponent self, SoundCueNodeData cueData, AbilitySystemComponent target)
        {
            if (self == null || cueData == null || cueData.soundClip == null)
            {
                return;
            }

            GameObject audioObject = target?.GetOwnerObject() ?? new GameObject("CueSound");
            AudioSource audioSource = audioObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = audioObject.AddComponent<AudioSource>();
            }

            audioSource.clip = cueData.soundClip;
            audioSource.volume = cueData.soundVolume;
            audioSource.loop = cueData.soundLoop;
            audioSource.Play();

            self.AttachedAudioSource = audioSource;
            if (audioSource.clip != null && !cueData.soundLoop)
            {
                self.Duration = audioSource.clip.length;
            }
        }

        public static bool PlayFloatingText(
            this ActiveCueComponent self,
            string text,
            Vector3 worldPosition,
            Color color,
            float fontSize,
            float duration,
            FloatingTextType textType)
        {
            if (self == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            SkillHudManager hudManager = SkillHudManager.GetOrCreate();
            int floatingTextHandle = hudManager.AddFloatingText(text, worldPosition, color, fontSize, duration, textType);
            if (floatingTextHandle <= 0)
            {
                return false;
            }

            self.FloatingTextHandle = floatingTextHandle;
            self.Duration = duration;
            return true;
        }

        public static void PlayParticleEffect(this ActiveCueComponent self, int particleEntityId, UGFEntityEffectInitData initData)
        {
            if (self == null || self.IsDisposed || particleEntityId <= 0)
            {
                self?.Expire();
                return;
            }

            self.ShowParticleEffectAsync(particleEntityId, initData).Forget();
        }

        private static async UniTaskVoid ShowParticleEffectAsync(this ActiveCueComponent self, int particleEntityId, UGFEntityEffectInitData initData)
        {
            if (self == null || self.IsDisposed || self.IsExpired)
            {
                return;
            }

            UGFEntityEffect effectEntity = self.AddChild<UGFEntityEffect, UGFEntityEffectInitData>(initData);
            await effectEntity.ShowEntityAsync(particleEntityId);

            if (self == null || self.IsDisposed || self.IsExpired)
            {
                if (!effectEntity.IsDisposed)
                {
                    effectEntity.Dispose();
                }

                return;
            }

            self.AttachedEffectEntity = effectEntity;
            if (!self.IsLooping)
            {
                self.Duration = effectEntity.GetParticleSystemDuration();
            }
        }

        private static void ReleaseRuntimeObjects(this ActiveCueComponent self)
        {
            UGFEntityEffect effectEntity = self.AttachedEffectEntity.As();
            if (effectEntity != null)
            {
                effectEntity.Dispose();
                self.AttachedEffectEntity = default;
            }

            if (self.AttachedAudioSource != null)
            {
                self.AttachedAudioSource.Stop();
                self.AttachedAudioSource = null;
            }

            if (self.FloatingTextHandle > 0)
            {
                SkillHudManager.Instance?.RemoveFloatingText(self.FloatingTextHandle);
                self.FloatingTextHandle = 0;
            }
        }
    }
}
