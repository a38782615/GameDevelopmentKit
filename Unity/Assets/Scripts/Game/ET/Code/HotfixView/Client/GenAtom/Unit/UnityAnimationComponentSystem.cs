using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(UnityAnimationComponent))]
    [FriendOf(typeof(UnityAnimationComponent))]
    public static partial class UnityAnimationComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnityAnimationComponent self)
        {
            self.Bind(string.Empty);
        }

        [EntitySystem]
        private static void Destroy(this UnityAnimationComponent self)
        {
            self.Animation = null;
            self.AnimationComponentPath = string.Empty;
        }

        public static void Bind(this UnityAnimationComponent self, string animationComponentPath)
        {
            Unit unit = self.GetParent<Unit>();
            GameObject viewGameObject = unit?.GetComponent<GameObjectComponent>()?.GameObject;
            if (viewGameObject == null)
            {
                self.Animation = null;
                self.AnimationComponentPath = string.Empty;
                return;
            }

            if (self.Animation != null && string.Equals(self.AnimationComponentPath, animationComponentPath, StringComparison.Ordinal))
            {
                return;
            }

            Animation[] animations = viewGameObject.GetComponentsInChildren<Animation>(true);
            self.Animation = FindAnimationComponent(animations, animationComponentPath, viewGameObject.transform);
            self.AnimationComponentPath = self.Animation == null ? string.Empty : GetTransformPath(self.Animation.transform, viewGameObject.transform);
        }

        public static bool PlayAnimation(this UnityAnimationComponent self, string name, bool loop, string animationComponentPath)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            self.Bind(animationComponentPath);
            if (self.Animation == null)
            {
                return false;
            }

            AnimationState state = ResolveAnimationState(self.Animation, name, out string resolvedName);
            if (state == null)
            {
                return false;
            }

            if (loop && self.Animation.IsPlaying(resolvedName) && state.wrapMode == WrapMode.Loop)
            {
                return true;
            }

            state.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
            state.time = 0f;
            self.Animation.wrapMode = state.wrapMode;
            return self.Animation.Play(resolvedName, PlayMode.StopSameLayer);
        }

        public static float GetAnimationLengthSeconds(this UnityAnimationComponent self, string name, string animationComponentPath)
        {
            if (string.IsNullOrEmpty(name))
            {
                return 0f;
            }

            self.Bind(animationComponentPath);
            AnimationState state = self.Animation == null ? null : ResolveAnimationState(self.Animation, name, out _);
            return state == null ? 0f : state.length;
        }

        private static AnimationState ResolveAnimationState(Animation animation, string name, out string resolvedName)
        {
            resolvedName = name;
            if (animation == null)
            {
                return null;
            }

            AnimationState state = animation[name];
            if (state != null || name != "Stand")
            {
                return state;
            }

            state = animation["Idle"];
            if (state != null)
            {
                resolvedName = "Idle";
            }

            return state;
        }

        private static Animation FindAnimationComponent(Animation[] animations, string targetPath, Transform root)
        {
            if (animations == null || animations.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(targetPath))
            {
                foreach (Animation animation in animations)
                {
                    if (animation == null)
                    {
                        continue;
                    }

                    if (string.Equals(GetTransformPath(animation.transform, root), targetPath, StringComparison.Ordinal))
                    {
                        return animation;
                    }
                }
            }

            return animations[0];
        }

        private static string GetTransformPath(Transform current, Transform root)
        {
            if (current == null)
            {
                return string.Empty;
            }

            if (current == root)
            {
                return current.name;
            }

            Stack<string> names = new Stack<string>();
            Transform cursor = current;
            while (cursor != null)
            {
                names.Push(cursor.name);
                if (cursor == root)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return string.Join("/", names.ToArray());
        }
    }
}
