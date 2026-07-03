using UnityEngine.UI;

namespace ET.Client
{
    [ComponentOf(typeof(UIFormLoginComponent))]
    public class UIWidgetMap : UGFUIWidget<MonoUIWidgetMap>, IAwake, IDestroy, IUGFUIWidgetOnOpen, IUGFUIWidgetOnClose
    {
        public Button[] StageButtons;
        public int[] StageSubLevels;
        public int StageSubLevelsLevel = -1;
    }
}
