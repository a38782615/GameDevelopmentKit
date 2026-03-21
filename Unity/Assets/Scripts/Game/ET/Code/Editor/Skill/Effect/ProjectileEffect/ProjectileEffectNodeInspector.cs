using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public class ProjectileEffectNodeInspector : EffectNodeInspector
    {
        protected override void BuildEffectInspectorUI(VisualElement container, SkillNodeBase node)
        {
            if (node is not ProjectileEffectNode projectileNode)
            {
                return;
            }

            ProjectileEffectNodeData data = projectileNode.TypedData;
            if (data == null)
            {
                return;
            }

            BuildLaunchSection(container, projectileNode, data);
            BuildTargetSection(container, projectileNode, data);
            BuildProjectileSection(container, projectileNode, data);
            BuildPierceSection(container, projectileNode, data);
            BuildCollisionSection(container, projectileNode, data);
            BuildBounceSection(container, projectileNode, data);
        }

        private void BuildLaunchSection(VisualElement container, ProjectileEffectNode projectileNode, ProjectileEffectNodeData data)
        {
            VisualElement launchSection = CreateCollapsibleSection("发射设置", out VisualElement launchContent, true);

            var launchSourceField = new EnumField("发射位置来源", data.launchPositionSource);
            ApplyEnumFieldStyle(launchSourceField);
            launchSourceField.RegisterValueChangedCallback(evt =>
            {
                data.launchPositionSource = (PositionSourceType)evt.newValue;
                projectileNode.SyncUIFromData();
            });
            launchContent.Add(launchSourceField);

            launchContent.Add(CreateTextField("发射挂点", data.launchBindingName, value =>
            {
                data.launchBindingName = value;
                projectileNode.SyncUIFromData();
            }));

            container.Add(launchSection);
        }

        private void BuildTargetSection(VisualElement container, ProjectileEffectNode projectileNode, ProjectileEffectNodeData data)
        {
            VisualElement targetSection = CreateCollapsibleSection("目标设置", out VisualElement targetContent, true);

            var targetSourceField = new EnumField("目标位置来源", data.targetPositionSource);
            ApplyEnumFieldStyle(targetSourceField);
            targetSourceField.RegisterValueChangedCallback(evt =>
            {
                data.targetPositionSource = (PositionSourceType)evt.newValue;
                projectileNode.SyncUIFromData();
            });
            targetContent.Add(targetSourceField);

            targetContent.Add(CreateTextField("目标挂点", data.targetBindingName, value =>
            {
                data.targetBindingName = value;
                projectileNode.SyncUIFromData();
            }));

            var targetTypeField = new EnumField("目标类型", data.projectileTargetType);
            ApplyEnumFieldStyle(targetTypeField);
            targetContent.Add(targetTypeField);

            var positionModeContainer = new VisualElement();
            var curveHeightContainer = new VisualElement();
            targetContent.Add(positionModeContainer);
            targetContent.Add(curveHeightContainer);

            void UpdateCurveHeightVisibility(ProjectileTargetType targetType, bool flyOver)
            {
                curveHeightContainer.Clear();
                if (targetType == ProjectileTargetType.Unit || !flyOver)
                {
                    curveHeightContainer.Add(CreateFloatField("曲线高度", data.curveHeight, value =>
                    {
                        data.curveHeight = value;
                        projectileNode.SyncUIFromData();
                    }));
                }
            }

            void UpdateTargetTypeUI(ProjectileTargetType targetType)
            {
                positionModeContainer.Clear();
                curveHeightContainer.Clear();

                if (targetType == ProjectileTargetType.Position)
                {
                    var flyOverToggle = new Toggle("飞越(穿过目标点)")
                    {
                        value = data.flyOver
                    };
                    flyOverToggle.style.marginTop = 4;
                    flyOverToggle.RegisterValueChangedCallback(evt =>
                    {
                        data.flyOver = evt.newValue;
                        projectileNode.SyncUIFromData();
                        UpdateCurveHeightVisibility(targetType, evt.newValue);
                    });
                    positionModeContainer.Add(flyOverToggle);

                    positionModeContainer.Add(CreateFloatField("偏移角度", data.offsetAngle, value =>
                    {
                        data.offsetAngle = value;
                        projectileNode.SyncUIFromData();
                    }));

                    UpdateCurveHeightVisibility(targetType, data.flyOver);
                    return;
                }

                UpdateCurveHeightVisibility(targetType, false);
            }

            targetTypeField.RegisterValueChangedCallback(evt =>
            {
                data.projectileTargetType = (ProjectileTargetType)evt.newValue;
                UpdateTargetTypeUI((ProjectileTargetType)evt.newValue);
                projectileNode.SyncUIFromData();
            });

            UpdateTargetTypeUI(data.projectileTargetType);
            container.Add(targetSection);
        }

        private void BuildProjectileSection(VisualElement container, ProjectileEffectNode projectileNode, ProjectileEffectNodeData data)
        {
            VisualElement projectileSection = CreateCollapsibleSection("投射物属性", out VisualElement projectileContent, true);

            projectileContent.Add(CreateIntField("实体ID", data.projectileEntityId, value =>
            {
                data.projectileEntityId = value;
                projectileNode.SyncUIFromData();
            }));

            var prefabField = new ObjectField("旧Prefab(兼容)")
            {
                objectType = typeof(GameObject),
                value = data.projectilePrefab
            };
            prefabField.RegisterValueChangedCallback(evt =>
            {
                data.projectilePrefab = evt.newValue as GameObject;
                projectileNode.SyncUIFromData();
            });
            projectileContent.Add(prefabField);

            projectileContent.Add(CreateFloatField("飞行速度", data.speed, value =>
            {
                data.speed = value;
                projectileNode.SyncUIFromData();
            }));

            var maxDistanceContainer = new VisualElement();
            projectileContent.Add(maxDistanceContainer);

            void UpdateMaxDistanceVisibility(ProjectileTargetType targetType)
            {
                maxDistanceContainer.Clear();
                if (targetType == ProjectileTargetType.Position)
                {
                    maxDistanceContainer.Add(CreateFloatField("最大距离(-1无限)", data.maxDistance, value =>
                    {
                        data.maxDistance = value;
                        projectileNode.SyncUIFromData();
                    }));
                }
            }

            UpdateMaxDistanceVisibility(data.projectileTargetType);

            projectileContent.Add(CreateFloatField("碰撞半径", data.collisionRadius, value =>
            {
                data.collisionRadius = value;
                projectileNode.SyncUIFromData();
            }));

            container.Add(projectileSection);
        }

        private void BuildPierceSection(VisualElement container, ProjectileEffectNode projectileNode, ProjectileEffectNodeData data)
        {
            VisualElement pierceSection = CreateCollapsibleSection("穿透设置", out VisualElement pierceContent, true);

            var isPiercingToggle = new Toggle("启用穿透")
            {
                value = data.isPiercing
            };
            pierceContent.Add(isPiercingToggle);

            var pierceCountContainer = new VisualElement();
            pierceContent.Add(pierceCountContainer);

            void UpdatePierceCountVisibility(bool isPiercing)
            {
                pierceCountContainer.Clear();
                if (isPiercing)
                {
                    pierceCountContainer.Add(CreateIntField("最大穿透数", data.maxPierceCount, value =>
                    {
                        data.maxPierceCount = value;
                        projectileNode.SyncUIFromData();
                    }));
                }
            }

            isPiercingToggle.RegisterValueChangedCallback(evt =>
            {
                data.isPiercing = evt.newValue;
                UpdatePierceCountVisibility(evt.newValue);
                projectileNode.SyncUIFromData();
            });

            UpdatePierceCountVisibility(data.isPiercing);
            container.Add(pierceSection);
        }

        private void BuildCollisionSection(VisualElement container, ProjectileEffectNode projectileNode, ProjectileEffectNodeData data)
        {
            VisualElement collisionTagSection = CreateCollapsibleSection("碰撞标签", out VisualElement collisionTagContent, true);

            collisionTagContent.Add(CreateTagSetField("目标标签", data.collisionTargetTags, value =>
            {
                data.collisionTargetTags = value;
                projectileNode.SyncUIFromData();
            }));

            collisionTagContent.Add(CreateTagSetField("排除标签", data.collisionExcludeTags, value =>
            {
                data.collisionExcludeTags = value;
                projectileNode.SyncUIFromData();
            }));

            container.Add(collisionTagSection);
        }

        private void BuildBounceSection(VisualElement container, ProjectileEffectNode projectileNode, ProjectileEffectNodeData data)
        {
            VisualElement bounceSection = CreateCollapsibleSection("反弹设置", out VisualElement bounceContent, true);

            var isBouncingToggle = new Toggle("启用反弹")
            {
                value = data.isBouncing
            };
            bounceContent.Add(isBouncingToggle);

            var bounceParamsContainer = new VisualElement();
            bounceContent.Add(bounceParamsContainer);

            void UpdateBounceParamsVisibility(bool isBouncing, BounceTargetMode mode)
            {
                bounceParamsContainer.Clear();
                if (!isBouncing)
                {
                    return;
                }

                var bounceModeField = new EnumField("反弹模式", data.bounceTargetMode);
                ApplyEnumFieldStyle(bounceModeField);
                bounceParamsContainer.Add(bounceModeField);

                bounceParamsContainer.Add(CreateIntField("最大反弹次数", data.maxBounceCount, value =>
                {
                    data.maxBounceCount = value;
                    projectileNode.SyncUIFromData();
                }));

                var modeParamsContainer = new VisualElement();
                bounceParamsContainer.Add(modeParamsContainer);

                void UpdateModeParams(BounceTargetMode currentMode)
                {
                    modeParamsContainer.Clear();
                    if (currentMode == BounceTargetMode.SearchNearest)
                    {
                        modeParamsContainer.Add(CreateFloatField("搜索半径", data.bounceSearchRadius, value =>
                        {
                            data.bounceSearchRadius = value;
                            projectileNode.SyncUIFromData();
                        }));

                        var canBounceToSameToggle = new Toggle("可反弹到已命中目标")
                        {
                            value = data.canBounceToSameTarget
                        };
                        canBounceToSameToggle.style.marginTop = 4;
                        canBounceToSameToggle.RegisterValueChangedCallback(evt =>
                        {
                            data.canBounceToSameTarget = evt.newValue;
                            projectileNode.SyncUIFromData();
                        });
                        modeParamsContainer.Add(canBounceToSameToggle);

                        var excludeSourceCampToggle = new Toggle("排除来源阵营")
                        {
                            value = data.excludeSourceCamp
                        };
                        excludeSourceCampToggle.style.marginTop = 4;
                        excludeSourceCampToggle.RegisterValueChangedCallback(evt =>
                        {
                            data.excludeSourceCamp = evt.newValue;
                            projectileNode.SyncUIFromData();
                        });
                        modeParamsContainer.Add(excludeSourceCampToggle);
                        return;
                    }

                    modeParamsContainer.Add(CreateFloatField("反弹偏移角度", data.bounceAngleOffset, value =>
                    {
                        data.bounceAngleOffset = value;
                        projectileNode.SyncUIFromData();
                    }));
                }

                bounceModeField.RegisterValueChangedCallback(evt =>
                {
                    data.bounceTargetMode = (BounceTargetMode)evt.newValue;
                    UpdateModeParams((BounceTargetMode)evt.newValue);
                    projectileNode.SyncUIFromData();
                });

                UpdateModeParams(mode);
            }

            isBouncingToggle.RegisterValueChangedCallback(evt =>
            {
                data.isBouncing = evt.newValue;
                UpdateBounceParamsVisibility(evt.newValue, data.bounceTargetMode);
                projectileNode.SyncUIFromData();
            });

            UpdateBounceParamsVisibility(data.isBouncing, data.bounceTargetMode);
            container.Add(bounceSection);
        }
    }
}
