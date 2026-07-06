using UnityEngine;

namespace ET.Client
{
    [ChildOf(typeof(GFEntityComponent))]
    public class GFEntityDropItem : UGFEntity<MonoGFEntityDropItem>, IAwake<Vector3>, IDestroy, IUGFEntityOnShow, IUGFEntityOnHide
    {
        public Vector3 Position;
    }
}
