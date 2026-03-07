using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 投射物视图桥接
    /// 仅用于兼容已挂载脚本的预制体和绘制调试 Gizmos
    /// </summary>
    [EnableClass]
    [FriendOfAttribute(typeof(ET.Client.ProjectileRuntimeComponent))]
    public class ProjectileController : MonoBehaviour
    {
        public EntityRef<ProjectileRuntimeComponent> Runtime;

        private void OnDrawGizmosSelected()
        {
            var runtime = this.Runtime.As();
            if (runtime == null || !runtime.IsInitialized) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(runtime.CurrentPosition, runtime.Data.CollisionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(runtime.EndPosition, 0.2f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(runtime.CurrentPosition, runtime.EndPosition);
        }
    }
}
