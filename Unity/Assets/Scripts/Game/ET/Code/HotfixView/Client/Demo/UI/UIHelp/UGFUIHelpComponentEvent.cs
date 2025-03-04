using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UGFUIHelpComponent))]
    public static partial class UGFUIHelpComponentSystem
    {
        [UGFUIFormEvent(UGFUIFormId.UIHelp)]
        public class UGFUIHelpEvent : AUGFUIFormEvent
        {
            public override void OnInit(UGFUIForm uiForm, object userData)
            {
                base.OnInit(uiForm, userData);
                UGFUIHelpComponent uiHelp = uiForm.AddComponent<UGFUIHelpComponent, Transform>(uiForm.transform);
            }
            
            public override void OnOpen(UGFUIForm uiForm, object userData)
            {
                base.OnOpen(uiForm, userData);
                
            }
        }
    }
}
