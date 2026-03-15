namespace ET
{
    public enum DelauLRSide
    {
        LEFT = 0,
        RIGHT
    }

    public class DelauSideHelper
    {
        public static DelauLRSide Other(DelauLRSide leftRight)
        {
            return leftRight == DelauLRSide.LEFT ? DelauLRSide.RIGHT : DelauLRSide.LEFT;
        }
    }
}