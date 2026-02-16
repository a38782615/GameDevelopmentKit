using UnityEditor.Experimental.GraphView;
using UnityEngine;



namespace ET.Client.Editor
{
    /// <summary>
    /// 投射物效果节点 - 简洁版，只显示端口
    /// 所有属性在Inspector面板绘制
    /// </summary>
    public class ProjectileEffectNode : EffectNode<ProjectileEffectNodeData>
    {
        // 输出端口
        private Port onHitPort;
        private Port onReachTargetPort;
        private Port onBouncePort;

        public ProjectileEffectNode(Vector2 position) : base(NodeType.ProjectileEffect, position) { }

        protected override string GetNodeTitle() => "投射物";
        protected override float GetNodeWidth() => 180;
        protected override bool ShowCompleteEffectPort => true;

        protected override void CreateEffectContent()
        {
            // 只创建输出端口
            onHitPort = CreateOutputPort("碰撞时");
            onReachTargetPort = CreateOutputPort("到达目标位置");
            onBouncePort = CreateOutputPort("反弹时");
        }

        protected override void SyncEffectContentFromData()
        {
            // 节点上没有UI控件需要同步
        }
    }
}
