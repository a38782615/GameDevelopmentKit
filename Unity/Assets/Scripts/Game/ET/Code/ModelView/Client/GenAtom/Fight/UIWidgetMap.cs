namespace ET.Client
{
    [ComponentOf(typeof(UIFormLoginComponent))]
    public class UIWidgetMap : UGFUIWidget<MonoUIWidgetMap>, IAwake, IDestroy, IUGFUIWidgetOnOpen, IUGFUIWidgetOnClose
    {
        public Game.ExButton[] StageButtons;
        public int[] StageButtonSubLevels;
        public int[] StageSubLevels;
        public int StageSubLevelsLevel = -1;
    }
}
