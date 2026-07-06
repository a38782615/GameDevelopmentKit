using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(RanDrawComponent))]
    [FriendOf(typeof(RanDrawComponent))]
    public static partial class RanDrawComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RanDrawComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this RanDrawComponent self)
        {
        }

        public static int GetIdx(this RanDrawComponent self, int[][] ints)
        {
            if (ints == null || ints.Length == 0)
            {
                return -1;
            }

            long totalWeight = 0;
            for (int i = 0; i < ints.Length; i++)
            {
                int[] item = ints[i];
                if (item == null || item.Length < 2 || item[1] <= 0)
                {
                    continue;
                }

                totalWeight += item[1];
            }

            if (totalWeight <= 0)
            {
                return -1;
            }

            long hitWeight = (long)(RandomGenerator.RandUInt64() % (ulong)totalWeight) + 1;
            long currentWeight = 0;
            for (int i = 0; i < ints.Length; i++)
            {
                int[] item = ints[i];
                if (item == null || item.Length < 2 || item[1] <= 0)
                {
                    continue;
                }

                currentWeight += item[1];
                if (hitWeight <= currentWeight)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int GetDropItem(this RanDrawComponent self, int[][] ints)
        {
            var idx = self.GetIdx(ints);
            if (idx < 0)
            {
                return -1;
            }

            return ints[idx][0];
        }
    }
}
