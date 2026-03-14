namespace ET
{
    [EnableClass]
    public static class ModeDefine
    {
#if D2
        [StaticField]
        public static readonly bool Is2D = true;
#else
        [StaticField]
        public static readonly bool Is2D = false;
#endif
    }
}
