using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ET.Client.Editor
{
    public static class SkillAssetPortIdMigrationTool
    {
        [MenuItem("SkillEditor/Automation/Migrate Connection Port IDs")]
        public static void MigrateAllSkillAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:SkillGraphData", new[] { SkillAssetTreeView.RootPath });
            var changedPaths = new List<string>();
            int changedAssets = 0;
            int changedConnections = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                SkillGraphData asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(path);
                if (asset == null)
                {
                    continue;
                }

                int beforeChangedConnections = CountCompletedConnections(asset.connections);
                bool connectionChanged = SkillConnectionPortIdUtility.NormalizeConnections(asset.nodes, asset.connections);
                bool nodeChanged = SkillConnectionPortIdUtility.NormalizeNodePortIds(asset.nodes);
                if (!connectionChanged && !nodeChanged)
                {
                    continue;
                }

                int afterChangedConnections = CountCompletedConnections(asset.connections);
                changedConnections += afterChangedConnections - beforeChangedConnections;
                changedAssets++;
                changedPaths.Add(path);
                EditorUtility.SetDirty(asset);
            }

            if (changedPaths.Count == 0)
            {
                Debug.Log("[SkillPortMigration] No asset required migration.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(changedPaths);
            AssetDatabase.Refresh();
            Debug.Log($"[SkillPortMigration] Migrated assets={changedAssets}, connections={changedConnections}");
        }

        private static int CountCompletedConnections(IList<ConnectionData> connections)
        {
            if (connections == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ConnectionData connection in connections)
            {
                if (connection != null &&
                    connection.outputPortId > SkillPortId.Invalid &&
                    connection.inputPortId > SkillPortId.Invalid)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
