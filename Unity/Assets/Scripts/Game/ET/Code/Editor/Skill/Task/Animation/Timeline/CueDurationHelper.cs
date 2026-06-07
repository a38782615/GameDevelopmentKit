using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ET.Client.Editor
{
    /// <summary>
    /// Cue 时长辅助工具
    /// </summary>
    public static class CueDurationHelper
    {
        private const string EntityAssetFormat = "Assets/Res/Entity/{0}.prefab";

#if UNITY_ET
        private const string CommonEntityXlsx = "../Design/Excel/ET/Datas/Game/Entity.xlsx";
#elif UNITY_GAMEHOT
        private const string CommonEntityXlsx = "../Design/Excel/GameHot/Datas/Game/Entity.xlsx";
#else
        private const string CommonEntityXlsx = "../Design/Excel/Game/Datas/Entity.xlsx";
#endif

#if UNITY_EDITOR
        private static Dictionary<int, string> s_EntityAssetNameCache;
#endif

        public static int GetCueDurationFrames(NodeData nodeData)
        {
            if (nodeData == null)
            {
                return -1;
            }

            float durationSeconds = -1f;
            if (nodeData is ParticleCueNodeData particleData)
            {
                durationSeconds = GetParticleDuration(particleData);
            }
            else if (nodeData is SoundCueNodeData soundData)
            {
                durationSeconds = GetSoundDuration(soundData);
            }

            if (durationSeconds <= 0f)
            {
                return -1;
            }

            return SkillEditorConstants.SecondsToFrames(durationSeconds);
        }

        public static float GetParticleDuration(ParticleCueNodeData data)
        {
            if (data == null || data.particleLoop)
            {
                return -1f;
            }

#if UNITY_EDITOR
            GameObject particlePrefab = data.particlePrefab;
            if (particlePrefab == null && data.particleEntityId > 0)
            {
                particlePrefab = GetParticleEntityPrefab(data.particleEntityId);
            }
            if (particlePrefab == null)
            {
                return -1f;
            }

            ParticleSystem[] particleSystems = particlePrefab.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems == null || particleSystems.Length == 0)
            {
                return -1f;
            }

            float maxDuration = 0f;
            foreach (ParticleSystem ps in particleSystems)
            {
                ParticleSystem.MainModule main = ps.main;
                if (main.loop)
                {
                    continue;
                }

                float totalDuration = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
                if (totalDuration > maxDuration)
                {
                    maxDuration = totalDuration;
                }
            }

            return maxDuration > 0f ? maxDuration : -1f;
#else
            return -1f;
#endif
        }

        public static float GetSoundDuration(SoundCueNodeData data)
        {
            if (data == null || data.soundClip == null || data.soundLoop)
            {
                return -1f;
            }

            return data.soundClip.length;
        }

#if UNITY_EDITOR
        private static GameObject GetParticleEntityPrefab(int entityId)
        {
            string assetName = GetEntityAssetName(entityId);
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(string.Format(EntityAssetFormat, assetName));
        }

        private static string GetEntityAssetName(int entityId)
        {
            if (s_EntityAssetNameCache == null)
            {
                s_EntityAssetNameCache = BuildEntityAssetNameCache();
            }

            return s_EntityAssetNameCache.TryGetValue(entityId, out string assetName) ? assetName : string.Empty;
        }

        private static Dictionary<int, string> BuildEntityAssetNameCache()
        {
            Dictionary<int, string> cache = new Dictionary<int, string>();
            if (!System.IO.File.Exists(CommonEntityXlsx))
            {
                return cache;
            }

            DataFormatter formatter = new DataFormatter();
            using (System.IO.FileStream stream = new System.IO.FileStream(CommonEntityXlsx, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                IWorkbook workbook = new XSSFWorkbook(stream);

                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    ISheet sheet = workbook.GetSheetAt(sheetIndex);
                    if (sheet == null || string.IsNullOrWhiteSpace(sheet.SheetName) || sheet.SheetName.StartsWith("~"))
                    {
                        continue;
                    }

                    for (int rowIndex = 3; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        IRow row = sheet.GetRow(rowIndex);
                        if (row == null)
                        {
                            continue;
                        }

                        if (!TryGetIntValue(row.GetCell(1), formatter, out int id) || id <= 0)
                        {
                            continue;
                        }

                        string assetName = formatter.FormatCellValue(row.GetCell(4));
                        if (string.IsNullOrWhiteSpace(assetName))
                        {
                            continue;
                        }

                        cache[id] = assetName;
                    }
                }
            }

            return cache;
        }

        private static bool TryGetIntValue(ICell cell, DataFormatter formatter, out int value)
        {
            value = 0;
            if (cell == null)
            {
                return false;
            }

            return int.TryParse(formatter.FormatCellValue(cell), out value);
        }
#endif
    }
}
