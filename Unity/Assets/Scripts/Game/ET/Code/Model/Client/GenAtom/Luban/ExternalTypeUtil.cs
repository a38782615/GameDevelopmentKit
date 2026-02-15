
namespace ET
{
    public static class ExternalTypeUtil
    {

        public static (float, float) NewFloat2(Float2 deserializeFloat2)
        {
            return (deserializeFloat2.Item1, deserializeFloat2.Item2);
        }

        public static (int, int) NewInt2(Int2 deserializeInt2)
        {
            return (deserializeInt2.Item1, deserializeInt2.Item2);
        }

    }
}