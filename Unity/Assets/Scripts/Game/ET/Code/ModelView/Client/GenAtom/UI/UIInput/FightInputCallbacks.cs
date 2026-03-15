using UnityEngine;
using UnityEngine.InputSystem;
using UGFLog = UnityGameFramework.Runtime.Log;

namespace ET.Client
{
    [EnableClass]
    [FriendOf(typeof(FightInputComponent))]
    public class FightInputCallbacks : InputControls.IRpgActions
    {
        private EntityWeakRef<FightInputComponent> owner;

        public FightInputCallbacks(FightInputComponent owner)
        {
            this.owner = owner;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            FightInputComponent self = this.owner;
            if (self == null || self.IsDisposed || !self.Enabled)
            {
                return;
            }

            self.MoveValue = context.ReadValue<Vector2>();
        }

        public void OnFire(InputAction.CallbackContext context)
        {
            FightInputComponent self = this.owner;
            if (self == null || self.IsDisposed || !self.Enabled)
            {
                return;
            }

            self.FirePressed = !context.canceled && context.ReadValueAsButton();
            if (context.performed)
            {
                self.FireTriggeredFrame = Time.frameCount;
                self.PendingScreenClick = true;
#if UNITY_EDITOR
                UGFLog.Info(
                    $"[FightInput] Fire performed frame={Time.frameCount} pointer=({self.PointerScreenPosition.x:0.##},{self.PointerScreenPosition.y:0.##})");
#endif
            }
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            FightInputComponent self = this.owner;
            if (self == null || self.IsDisposed || !self.Enabled)
            {
                return;
            }

            self.CancelPressed = !context.canceled && context.ReadValueAsButton();
            if (context.performed)
            {
                self.CancelTriggeredFrame = Time.frameCount;
            }
        }

        public void OnRotateR(InputAction.CallbackContext context)
        {
            FightInputComponent self = this.owner;
            if (self == null || self.IsDisposed || !self.Enabled)
            {
                return;
            }

            self.RotateRPressed = !context.canceled && context.ReadValueAsButton();
            if (context.performed)
            {
                self.RotateRTriggeredFrame = Time.frameCount;
            }
        }
    }
}
