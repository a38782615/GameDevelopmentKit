using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(FightInputComponent))]
    [FriendOf(typeof(FightInputComponent))]
    public static partial class FightInputComponentSystem
    {
        private const float MoveInputDeadZone = 0.01f;
        private const float KeyboardMoveTargetDistance = 1000f;

        [EntitySystem]
        private static void Awake(this FightInputComponent self)
        {
            self.InputControls = new InputControls();
            self.Callbacks = new FightInputCallbacks(self);
            self.InputControls.rpg.SetCallbacks(self.Callbacks);
            self.InputControls.rpg.Enable();
            self.RefreshPointerPosition();
        }

        [EntitySystem]
        private static void Update(this FightInputComponent self)
        {
            if (!self.Enabled || self.InputControls == null)
            {
                return;
            }

            self.MoveValue = self.InputControls.rpg.Move.ReadValue<Vector2>();
            self.RefreshPointerPosition();
            self.PublishPendingScreenClick();
            self.UpdateKeyboardMove();
        }

        [EntitySystem]
        private static void Destroy(this FightInputComponent self)
        {
            self.DisableInputControls();
            self.Callbacks = null;
            self.InputControls = null;
            self.MoveValue = Vector2.zero;
            self.LastKeyboardMoveDirection = Vector2.zero;
            self.PointerScreenPosition = Vector2.zero;
            self.FirePressed = false;
            self.CancelPressed = false;
            self.RotateRPressed = false;
            self.PendingScreenClick = false;
            self.IsKeyboardMoving = false;
            self.FireTriggeredFrame = -1;
            self.CancelTriggeredFrame = -1;
            self.RotateRTriggeredFrame = -1;
        }

        public static bool WasFireTriggeredThisFrame(this FightInputComponent self)
        {
            return self != null && !self.IsDisposed && self.FireTriggeredFrame == Time.frameCount;
        }

        public static bool WasCancelTriggeredThisFrame(this FightInputComponent self)
        {
            return self != null && !self.IsDisposed && self.CancelTriggeredFrame == Time.frameCount;
        }

        public static bool WasRotateRTriggeredThisFrame(this FightInputComponent self)
        {
            return self != null && !self.IsDisposed && self.RotateRTriggeredFrame == Time.frameCount;
        }

        public static void SetInputEnabled(this FightInputComponent self, bool enabled)
        {
            if (self == null || self.IsDisposed || self.Enabled == enabled)
            {
                return;
            }

            self.Enabled = enabled;
            if (enabled)
            {
                self.InputControls?.rpg.Enable();
                self.RefreshPointerPosition();
                return;
            }

            self.InputControls?.rpg.Disable();
            self.StopKeyboardMove();
            self.MoveValue = Vector2.zero;
            self.LastKeyboardMoveDirection = Vector2.zero;
            self.FirePressed = false;
            self.CancelPressed = false;
            self.RotateRPressed = false;
            self.PendingScreenClick = false;
            self.IsKeyboardMoving = false;
            self.FireTriggeredFrame = -1;
            self.CancelTriggeredFrame = -1;
            self.RotateRTriggeredFrame = -1;
        }

        private static void DisableInputControls(this FightInputComponent self)
        {
            if (self.InputControls == null)
            {
                return;
            }

            if (self.Callbacks != null)
            {
                self.InputControls.rpg.RemoveCallbacks(self.Callbacks);
            }

            self.InputControls.rpg.Disable();
            self.InputControls.Dispose();
        }

        private static void RefreshPointerPosition(this FightInputComponent self)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                self.PointerScreenPosition = Vector2.zero;
                return;
            }

            self.PointerScreenPosition = mouse.position.ReadValue();
        }

        private static void PublishPendingScreenClick(this FightInputComponent self)
        {
            if (self == null || self.IsDisposed || !self.Enabled || !self.PendingScreenClick)
            {
                return;
            }

            Scene scene = self.GetParent<Scene>();
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            self.PendingScreenClick = false;
            self.RefreshPointerPosition();
            if (self.IsPointerBlockedByUI())
            {
                return;
            }
            EventSystem.Instance.Publish(scene, new FightInputScreenClick
            {
                ScreenPosition = new float2(self.PointerScreenPosition.x, self.PointerScreenPosition.y),
            });
        }

        private static bool IsPointerBlockedByUI(this FightInputComponent self)
        {
            global::UnityEngine.EventSystems.EventSystem currentEventSystem = global::UnityEngine.EventSystems.EventSystem.current;
            if (currentEventSystem == null)
            {
                return false;
            }

            return currentEventSystem.IsPointerOverGameObject();
        }

        private static void UpdateKeyboardMove(this FightInputComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            Unit unit = scene == null || scene.IsDisposed ? null : UnitHelper.GetMyUnitFromCurrentScene(scene);
            if (unit == null || unit.IsDisposed || !unit.IsMoveAllowed())
            {
                self.StopKeyboardMove(unit);
                return;
            }

            Vector2 moveInput = self.MoveValue;
            float2 planarDirection = new float2(moveInput.x, moveInput.y);
            if (math.lengthsq(planarDirection) <= MoveInputDeadZone * MoveInputDeadZone)
            {
                self.StopKeyboardMove(unit);
                return;
            }

            float2 normalizedDirection = math.normalize(planarDirection);
            Vector2 normalizedDirectionVector2 = new Vector2(normalizedDirection.x, normalizedDirection.y);
            if (self.IsKeyboardMoving && Vector2.Dot(self.LastKeyboardMoveDirection, normalizedDirectionVector2) >= 0.999f)
            {
                return;
            }

            global::ET.AttributeComponent attributeComponent = unit.GetComponent<global::ET.AttributeComponent>();
            float speed = attributeComponent?.GetCurrentValue(global::ET.NumericType.Speed) ?? 0f;
            if (speed <= 0f)
            {
                self.StopKeyboardMove(unit);
                return;
            }

            float2 currentPosition = unit.Position.ToPlanar();
            float2 targetPosition = currentPosition + normalizedDirection * KeyboardMoveTargetDistance;
            using ListComponent<float3> path = ListComponent<float3>.Create();
            path.Add(currentPosition.ToModePosition());
            path.Add(targetPosition.ToModePosition());
            unit.MoveAlongPathAsync(path, speed).Forget();
            self.LastKeyboardMoveDirection = normalizedDirectionVector2;
            self.IsKeyboardMoving = true;
        }

        private static void StopKeyboardMove(this FightInputComponent self, Unit unit = null)
        {
            if (!self.IsKeyboardMoving)
            {
                return;
            }

            unit ??= self.GetParent<Scene>() == null ? null : UnitHelper.GetMyUnitFromCurrentScene(self.GetParent<Scene>());
            if (unit != null && !unit.IsDisposed)
            {
                unit.StopMove(true);
            }

            self.IsKeyboardMoving = false;
            self.LastKeyboardMoveDirection = Vector2.zero;
        }
    }
}
