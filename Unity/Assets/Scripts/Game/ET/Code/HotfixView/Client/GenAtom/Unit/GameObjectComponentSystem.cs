using System;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameObjectComponent))]
    public static partial class GameObjectComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GameObjectComponent self)
        {
            UnityEngine.GameObject gameObject = self.GameObject;
            if (gameObject == null)
            {
                return;
            }

            if (gameObject.GetComponent("UnityGameFramework.Runtime.Entity") == null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }

            self.GameObject = null;
        }
        
        [EntitySystem]
        private static void Awake(this GameObjectComponent self)
        {

        }
    }
}
