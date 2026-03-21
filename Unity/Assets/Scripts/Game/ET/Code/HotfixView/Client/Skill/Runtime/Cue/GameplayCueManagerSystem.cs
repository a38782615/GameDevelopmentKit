using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayCueManager))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayCueManager))]
    public static partial class GameplayCueManagerSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayCueManager self)
        {
            self.LastTickFrame = -1;
            self.ActiveCues.Clear();
            self.PendingRemoval.Clear();
        }

        [EntitySystem]
        private static void Update(this GameplayCueManager self)
        {
            self.TickOncePerFrame(UnityEngine.Time.deltaTime);
        }

        [EntitySystem]
        private static void Destroy(this GameplayCueManager self)
        {
            self.Clear();
        }

        public static GameplayCueManager GetGameplayCueManager(this GameplayCueSpec self)
        {
            return self?.Root()?.CurrentScene()?.GetComponent<GameplayCueManager>();
        }

        public static ActiveGameplayCue PlayParticleCue(this GameplayCueManager self, ParticleCueNodeData cueData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (self == null || cueData == null)
            {
                return null;
            }

            Vector3 position = Vector3.zero;
            Transform parent = null;
            float facingDirection = 1f;

            if (target?.Owner != null)
            {
                Transform targetTransform = target.Owner.transform;
                facingDirection = targetTransform.localScale.x >= 0 ? 1f : -1f;

                if (!string.IsNullOrEmpty(cueData.particleBindingName))
                {
                    Transform bindingPoint = targetTransform.Find(cueData.particleBindingName);
                    if (bindingPoint != null)
                    {
                        targetTransform = bindingPoint;
                    }
                }

                Vector3 adjustedOffset = cueData.particleOffset;
                adjustedOffset.x *= facingDirection;
                position = targetTransform.position + adjustedOffset;

                if (cueData.attachToTarget)
                {
                    parent = targetTransform;
                }
            }

            Vector3 scale = cueData.particleScale;
            scale.x *= facingDirection;
            return self.PlayParticleCue(cueData.particleEntityId, position, scale, parent, cueData.particleLoop);
        }

        public static ActiveGameplayCue PlayParticleCue(this GameplayCueManager self, int particleEntityId, Vector3 position, Vector3 scale, Transform attachTransform, bool isLoop)
        {
            if (self == null || self.IsDisposed || particleEntityId <= 0)
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = isLoop
            };

            UGFEntityEffectInitData initData = new UGFEntityEffectInitData
            {
                Position = position,
                Scale = scale,
                AttachTransform = attachTransform
            };

            self.ShowParticleCueEntityAsync(activeCue, particleEntityId, initData, isLoop).Forget();
            self.ActiveCues.Add(activeCue);
            return activeCue;
        }

        public static ActiveGameplayCue PlaySoundCue(this GameplayCueManager self, SoundCueNodeData cueData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (self == null || cueData == null || cueData.soundClip == null)
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = cueData.soundLoop
            };

            AudioSource audioSource = self.PlaySound(cueData, target);
            if (audioSource != null)
            {
                activeCue.AttachedAudioSource = audioSource;

                if (audioSource.clip != null && !cueData.soundLoop)
                {
                    activeCue.Duration = audioSource.clip.length;
                }
            }

            self.ActiveCues.Add(activeCue);
            return activeCue;
        }

        public static ActiveGameplayCue PlayFloatingTextCue(this GameplayCueManager self, string text, Vector3 worldPosition, Color color, float fontSize, float duration, FloatingTextType textType)
        {
            if (self == null || string.IsNullOrEmpty(text))
            {
                return null;
            }

            SkillHudManager hudManager = SkillHudManager.GetOrCreate();
            int floatingTextHandle = hudManager.AddFloatingText(text, worldPosition, color, fontSize, duration, textType);
            if (floatingTextHandle <= 0)
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = false,
                Duration = duration
            };
            activeCue.OnRemoved += () =>
            {
                hudManager.RemoveFloatingText(floatingTextHandle);
            };

            self.ActiveCues.Add(activeCue);
            return activeCue;
        }

        public static void StopCue(this GameplayCueManager self, ActiveGameplayCue activeCue)
        {
            if (self == null || activeCue == null)
            {
                return;
            }

            activeCue.Stop();
            self.PendingRemoval.Add(activeCue);
        }

        public static void Tick(this GameplayCueManager self, float deltaTime)
        {
            foreach (ActiveGameplayCue cue in self.ActiveCues)
            {
                cue.Tick(deltaTime);

                if (cue.IsExpired)
                {
                    self.PendingRemoval.Add(cue);
                }
            }

            if (self.PendingRemoval.Count <= 0)
            {
                return;
            }

            foreach (ActiveGameplayCue cue in self.PendingRemoval)
            {
                cue.Stop();
                self.ActiveCues.Remove(cue);
            }

            self.PendingRemoval.Clear();
        }

        public static void TickOncePerFrame(this GameplayCueManager self, float deltaTime)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            int frameCount = Time.frameCount;
            if (self.LastTickFrame == frameCount)
            {
                return;
            }

            self.LastTickFrame = frameCount;
            self.Tick(deltaTime);
        }

        public static void Clear(this GameplayCueManager self)
        {
            if (self == null)
            {
                return;
            }

            foreach (ActiveGameplayCue cue in self.ActiveCues)
            {
                cue.Stop();
            }

            self.ActiveCues.Clear();
            self.PendingRemoval.Clear();
        }

        private static async UniTaskVoid ShowParticleCueEntityAsync(this GameplayCueManager self, ActiveGameplayCue activeCue, int particleEntityId, UGFEntityEffectInitData initData, bool isLoop)
        {
            if (self == null || self.IsDisposed || activeCue == null || activeCue.IsExpired)
            {
                return;
            }

            UGFEntityEffect effectEntity = self.AddChild<UGFEntityEffect, UGFEntityEffectInitData>(initData);
            try
            {
                await effectEntity.ShowEntityAsync(particleEntityId);
            }
            catch (System.Exception e)
            {
                Log.Error($"[GameplayCue] Show particle effect entity failed. entityId={particleEntityId} error={e}");
                if (!effectEntity.IsDisposed)
                {
                    effectEntity.Dispose();
                }

                activeCue.Expire();
                return;
            }

            if (self == null || self.IsDisposed || activeCue == null)
            {
                if (!effectEntity.IsDisposed)
                {
                    effectEntity.Dispose();
                }

                return;
            }

            if (activeCue.IsExpired)
            {
                if (!effectEntity.IsDisposed)
                {
                    effectEntity.Dispose();
                }

                return;
            }

            activeCue.AttachedEffectEntity = effectEntity;
            activeCue.AttachedObject = effectEntity.CachedTransform != null ? effectEntity.CachedTransform.gameObject : null;
            if (!isLoop && activeCue.AttachedObject != null)
            {
                activeCue.Duration = self.GetParticleSystemDuration(activeCue.AttachedObject);
            }
        }

        private static float GetParticleSystemDuration(this GameplayCueManager self, GameObject particleObject)
        {
            ParticleSystem[] particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>();
            if (particleSystems == null || particleSystems.Length == 0)
            {
                return 0f;
            }

            float maxDuration = 0f;
            foreach (ParticleSystem ps in particleSystems)
            {
                ParticleSystem.MainModule main = ps.main;
                if (main.loop)
                {
                    continue;
                }

                float totalDuration = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
                if (totalDuration > maxDuration)
                {
                    maxDuration = totalDuration;
                }
            }

            return maxDuration;
        }

        private static AudioSource PlaySound(this GameplayCueManager self, SoundCueNodeData cueData, AbilitySystemComponent target)
        {
            AudioClip clip = cueData.soundClip;
            if (clip == null)
            {
                return null;
            }

            GameObject audioObject = target?.Owner != null ? target.Owner : new GameObject("CueSound");
            AudioSource audioSource = audioObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = audioObject.AddComponent<AudioSource>();
            }

            audioSource.clip = clip;
            audioSource.volume = cueData.soundVolume;
            audioSource.loop = cueData.soundLoop;
            audioSource.Play();
            return audioSource;
        }
    }
}
