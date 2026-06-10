using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(AnimationManagerComponent))]
    public static partial class AbilitySystemComponentSystem
    {
        private const string BeAttackAnimationName = "BeAttack";
        private const string DeathAnimationName = "Death";
        private const int DefaultBeAttackRecoverDelayMs = 500;
        private const int MinBeAttackRecoverDelayMs = 100;
        private const int MaxBeAttackRecoverDelayMs = 1500;
        private const int DefaultDeathRemoveDelayMs = 1000;
        private const int MinDeathRemoveDelayMs = 300;
        private const int MaxDeathRemoveDelayMs = 3000;

        public static void PlayBeAttackPresentation(this AbilitySystemComponent self)
        {
            PlayBeAttackPresentationAsync(self).Forget();
        }

        private static async UniTaskVoid PlayBeAttackPresentationAsync(AbilitySystemComponent self)
        {
            Unit unit = self.GetParent<SkillUnit>()?.Unit.As();
            AnimationManagerComponent animationManager = unit?.GetOrAddComponent<AnimationManagerComponent>();
            if (animationManager == null)
            {
                return;
            }

            bool played = animationManager.PlayAnimation(BeAttackAnimationName, false);
            if (!played && !string.Equals(animationManager.CurrentAnimationName, BeAttackAnimationName, System.StringComparison.Ordinal))
            {
                return;
            }

            long animationVersion = animationManager.AnimationVersion;
            int delayMs = animationManager.GetAnimationLengthMs(BeAttackAnimationName, string.Empty, DefaultBeAttackRecoverDelayMs);
            delayMs = Mathf.Clamp(delayMs, MinBeAttackRecoverDelayMs, MaxBeAttackRecoverDelayMs);
            SkillDiagFileLogger.Log($"[Animation] Play name={BeAttackAnimationName} asc={self.InstanceId} unit={unit.Id} recoverDelayMs={delayMs}");

            TimerComponent timerComponent = self.Root()?.GetComponent<TimerComponent>();
            if (timerComponent != null)
            {
                await timerComponent.WaitAsync(delayMs);
            }
            else
            {
                await UniTask.Delay(delayMs);
            }

            if (self.IsDisposed || unit.IsDisposed || !self.IsAlive())
            {
                return;
            }

            AnimationManagerComponent currentAnimationManager = unit.GetComponent<AnimationManagerComponent>();
            if (currentAnimationManager == null
                || currentAnimationManager.IsStunned
                || currentAnimationManager.AnimationVersion != animationVersion
                || !string.Equals(currentAnimationManager.CurrentAnimationName, BeAttackAnimationName, System.StringComparison.Ordinal))
            {
                return;
            }

            currentAnimationManager.PlayLocomotionAnimation();
        }

        public static void PlayDeathPresentationAndRemove(this AbilitySystemComponent self)
        {
            PlayDeathPresentationAndRemoveAsync(self).Forget();
        }

        private static async UniTaskVoid PlayDeathPresentationAndRemoveAsync(AbilitySystemComponent self)
        {
            SkillUnit skillUnit = self?.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            Scene scene = unit?.Scene();
            Scene root = self?.Root();
            if (unit == null || scene == null || root == null)
            {
                return;
            }

            long unitId = unit.Id;
            long ascInstanceId = self.InstanceId;
            unit.StopMove(false);
            unit.RemoveComponent<EntityBody>();

            AnimationManagerComponent animationManager = unit.GetOrAddComponent<AnimationManagerComponent>();
            animationManager.PlayAnimation(DeathAnimationName, false);
            int delayMs = animationManager.GetAnimationLengthMs(DeathAnimationName, string.Empty, DefaultDeathRemoveDelayMs);
            delayMs = Mathf.Clamp(delayMs, MinDeathRemoveDelayMs, MaxDeathRemoveDelayMs);
            SkillDiagFileLogger.Log($"[Animation] Play name={DeathAnimationName} asc={ascInstanceId} unit={unitId} removeDelayMs={delayMs}");

            TimerComponent timerComponent = root.GetComponent<TimerComponent>();
            if (timerComponent != null)
            {
                await timerComponent.WaitAsync(delayMs);
            }
            else
            {
                await UniTask.Delay(delayMs);
            }

            if (scene.IsDisposed || unit.IsDisposed)
            {
                return;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent?.Get(unitId) != unit)
            {
                return;
            }

            SkillDiagFileLogger.Log($"[Death] RemoveUnit asc={ascInstanceId} unit={unitId}");
            unitComponent.Remove(unitId);
        }
    }
}
