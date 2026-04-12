using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class FightInputComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public InputControls InputControls;
        public FightInputCallbacks Callbacks;
        public Vector2 MoveValue;
        public Vector2 LastKeyboardMoveDirection;
        public Vector2 PointerScreenPosition;
        public bool FirePressed;
        public bool CancelPressed;
        public bool RotateRPressed;
        public bool PendingScreenClick;
        public bool IsKeyboardMoving;
        public int FireTriggeredFrame = -1;
        public int CancelTriggeredFrame = -1;
        public int RotateRTriggeredFrame = -1;
        public bool Enabled = true;
    }
}
