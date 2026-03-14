using System.Collections.Generic;

using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// Cue 管理器，负责管理所有激活的 ActiveGameplayCue（View 层资源）。
    /// 负责粒子、音效、飘字等底层资源的播放、更新和清理。
    /// 注意：GameplayCueSpec 的生命周期由 GameplayCueContainerComponent 管理。
    /// 此类只管理 ActiveGameplayCue 实例（粒子对象、音效源等）。
    /// Tick 由 GameplayCueContainerComponent 或外部驱动。
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    public class GameplayCueManager : Singleton<GameplayCueManager>
    {
        private int m_LastTickFrame = -1;

        /// <summary>
        /// 所有激活的 Cue。
        /// </summary>
        private readonly List<ActiveGameplayCue> m_ActiveCues = new List<ActiveGameplayCue>();

        /// <summary>
        /// 待移除的 Cue 列表。
        /// </summary>
        private readonly List<ActiveGameplayCue> m_PendingRemoval = new List<ActiveGameplayCue>();

        public static GameplayCueManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameplayCueManager manager = new GameplayCueManager();
            World.Instance.AddSingleton(manager);
            return manager;
        }

        // ============ 公共方法 ============

        /// <summary>
        /// 播放粒子 Cue。
        /// </summary>
        public ActiveGameplayCue PlayParticleCue(ParticleCueNodeData cueData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (cueData == null)
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = cueData.particleLoop
            };

            GameObject particleObject = this.LoadAndInstantiateParticle(cueData, target);
            if (particleObject != null)
            {
                activeCue.AttachedObject = particleObject;

                if (!cueData.particleLoop)
                {
                    activeCue.Duration = this.GetParticleSystemDuration(particleObject);
                }
            }

            this.m_ActiveCues.Add(activeCue);
            return activeCue;
        }

        /// <summary>
        /// 按位置和参数播放粒子 Cue。
        /// </summary>
        public ActiveGameplayCue PlayParticleCue(
            GameObject prefab,
            Vector3 position,
            Vector3 scale,
            Transform attachTransform,
            bool isLoop)
        {
            if (prefab == null)
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = isLoop
            };

            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, attachTransform);
            instance.transform.localScale = scale;

            activeCue.AttachedObject = instance;

            if (!isLoop)
            {
                activeCue.Duration = this.GetParticleSystemDuration(instance);
            }

            this.m_ActiveCues.Add(activeCue);
            return activeCue;
        }

        /// <summary>
        /// 播放音效 Cue。
        /// </summary>
        public ActiveGameplayCue PlaySoundCue(SoundCueNodeData cueData, AbilitySystemComponent source, AbilitySystemComponent target)
        {
            if (cueData == null || cueData.soundClip == null)
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = cueData.soundLoop
            };

            AudioSource audioSource = this.PlaySound(cueData, target);
            if (audioSource != null)
            {
                activeCue.AttachedAudioSource = audioSource;

                if (audioSource.clip != null && !cueData.soundLoop)
                {
                    activeCue.Duration = audioSource.clip.length;
                }
            }

            this.m_ActiveCues.Add(activeCue);
            return activeCue;
        }

        /// <summary>
        /// 播放飘字 Cue。
        /// </summary>
        public ActiveGameplayCue PlayFloatingTextCue(
            string text,
            Vector3 worldPosition,
            Color color,
            float fontSize,
            float duration,
            FloatingTextType textType)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            ActiveGameplayCue activeCue = new ActiveGameplayCue
            {
                IsLooping = false,
                Duration = duration
            };

            if (FloatingTextManager.Instance != null)
            {
                GameObject floatingTextObject = FloatingTextManager.Instance.CreateFloatingText(
                    text,
                    worldPosition,
                    color,
                    fontSize,
                    duration,
                    textType);

                if (floatingTextObject != null)
                {
                    activeCue.AttachedObject = floatingTextObject;
                }
            }
            else
            {
                Debug.LogWarning("[GameplayCueManager] FloatingTextManager 未初始化，请在场景中添加 FloatingTextManager 组件");
            }

            this.m_ActiveCues.Add(activeCue);
            return activeCue;
        }

        /// <summary>
        /// 停止指定的 Cue。
        /// </summary>
        public void StopCue(ActiveGameplayCue activeCue)
        {
            if (activeCue == null)
            {
                return;
            }

            activeCue.Stop();
            this.m_PendingRemoval.Add(activeCue);
        }

        /// <summary>
        /// 每帧更新。
        /// </summary>
        public void Tick(float deltaTime)
        {
            foreach (ActiveGameplayCue cue in this.m_ActiveCues)
            {
                cue.Tick(deltaTime);

                if (cue.IsExpired)
                {
                    this.m_PendingRemoval.Add(cue);
                }
            }

            if (this.m_PendingRemoval.Count > 0)
            {
                foreach (ActiveGameplayCue cue in this.m_PendingRemoval)
                {
                    cue.Stop();
                    this.m_ActiveCues.Remove(cue);
                }

                this.m_PendingRemoval.Clear();
            }
        }

        public void TickOncePerFrame(float deltaTime)
        {
            int frameCount = Time.frameCount;
            if (this.m_LastTickFrame == frameCount)
            {
                return;
            }

            this.m_LastTickFrame = frameCount;
            this.Tick(deltaTime);
        }

        /// <summary>
        /// 清理所有 Cue。
        /// </summary>
        public void Clear()
        {
            foreach (ActiveGameplayCue cue in this.m_ActiveCues)
            {
                cue.Stop();
            }

            this.m_ActiveCues.Clear();
            this.m_PendingRemoval.Clear();
        }

        protected override void Destroy()
        {
            this.Clear();
        }

        // ============ 私有方法 ============

        /// <summary>
        /// 加载并实例化粒子特效。
        /// </summary>
        private GameObject LoadAndInstantiateParticle(ParticleCueNodeData cueData, AbilitySystemComponent target)
        {
            if (cueData.particlePrefab == null)
            {
                return null;
            }

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
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
                rotation = targetTransform.rotation;

                if (cueData.attachToTarget)
                {
                    parent = targetTransform;
                }
            }

            GameObject instance = UnityEngine.Object.Instantiate(cueData.particlePrefab, position, rotation, parent);

            Vector3 scale = cueData.particleScale;
            scale.x *= facingDirection;
            instance.transform.localScale = scale;

            return instance;
        }

        /// <summary>
        /// 获取粒子系统的总时长，包括所有子节点。
        /// </summary>
        private float GetParticleSystemDuration(GameObject particleObject)
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

                float startDelay = main.startDelay.constantMax;
                float duration = main.duration;
                float startLifetime = main.startLifetime.constantMax;
                float totalDuration = startDelay + duration + startLifetime;

                if (totalDuration > maxDuration)
                {
                    maxDuration = totalDuration;
                }
            }

            return maxDuration;
        }

        /// <summary>
        /// 播放音效。
        /// </summary>
        private AudioSource PlaySound(SoundCueNodeData cueData, AbilitySystemComponent target)
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
