using System.Collections.Generic;

namespace ET.Client.Editor
{
    public static class SkillConnectionPortIdUtility
    {
        public static bool NormalizeConnections(IReadOnlyList<NodeData> nodes, IList<ConnectionData> connections)
        {
            if (nodes == null || connections == null)
            {
                return false;
            }
            return false;
        }

        public static bool NormalizeNodePortIds(IReadOnlyList<NodeData> nodes)
        {
            if (nodes == null)
            {
                return false;
            }

            bool changed = false;
            foreach (NodeData node in nodes)
            {
                switch (node)
                {
                    case AnimationNodeData animationNode:
                        changed |= NormalizeAnimationNode(animationNode);
                        break;
                    case AbilityNodeData abilityNode:
                        changed |= NormalizeAbilityNode(abilityNode);
                        break;
                }
            }

            return changed;
        }

        private static bool NormalizeAnimationNode(AnimationNodeData animationNode)
        {
            bool changed = false;

            if (animationNode?.timeEffects != null)
            {
                foreach (TimeEffectData timeEffect in animationNode.timeEffects)
                {
                    if (timeEffect == null)
                    {
                        continue;
                    }

                    int resolvedPortId = ResolveAnimationPortId(timeEffect.portIdValue, timeEffect.legacyPortId);
                    if (resolvedPortId > SkillPortId.Invalid && timeEffect.portIdValue != resolvedPortId)
                    {
                        timeEffect.portIdValue = resolvedPortId;
                        changed = true;
                    }
                }
            }

            if (animationNode?.timeCues != null)
            {
                foreach (TimeCueData timeCue in animationNode.timeCues)
                {
                    if (timeCue == null)
                    {
                        continue;
                    }

                    int resolvedPortId = ResolveAnimationPortId(timeCue.portIdValue, timeCue.legacyPortId);
                    if (resolvedPortId > SkillPortId.Invalid && timeCue.portIdValue != resolvedPortId)
                    {
                        timeCue.portIdValue = resolvedPortId;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool NormalizeAbilityNode(AbilityNodeData abilityNode)
        {
            if (abilityNode?.eventOutputPorts == null)
            {
                return false;
            }

            bool changed = false;
            foreach (AbilityEventPortData portData in abilityNode.eventOutputPorts)
            {
                if (portData == null)
                {
                    continue;
                }

                int resolvedPortId = !string.IsNullOrEmpty(portData.legacyPortId)
                    ? SkillPortIdUtility.ResolveAbilityEventPortId(portData.legacyPortId)
                    : SkillPortIdUtility.ResolveAbilityEventPortId(portData.eventType, portData.customEventTag);
                if (resolvedPortId > SkillPortId.Invalid && portData.portIdValue != resolvedPortId)
                {
                    portData.portIdValue = resolvedPortId;
                    changed = true;
                }
            }

            return changed;
        }

        private static int ResolveAnimationPortId(int currentPortId, string legacyPortId)
        {
            if (!string.IsNullOrEmpty(legacyPortId))
            {
                return SkillPortIdUtility.ResolveAnimationTrackPortId(legacyPortId);
            }

            return currentPortId;
        }
    }
}
