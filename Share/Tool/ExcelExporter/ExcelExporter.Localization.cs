﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Bright.Serialization;
using ExcelDataReader;
using GameFramework.Localization;

namespace ET
{
    public static partial class ExcelExporter
    {
        public static class ExcelExporter_Localization
        {
            private static readonly string LocalizationOutPutDir = Path.GetFullPath("../Unity/Assets/Res/Localization");
            private static readonly string AssetUtilityCodeFile = Path.GetFullPath("../Unity/Assets/Scripts/Game/Generate/Localization/AssetUtility.Localization.cs");
            private static readonly string LocalizationReadyLanguageCodeFile = Path.GetFullPath("../Unity/Assets/Scripts/Game/Editor/Generate/Localization/LocalizationReadyLanguage.cs");
            
            private struct LanguageTableInfo
            {
                public int columnIndex;
                public Language language;
            }
            
            private struct KeyTableInfo
            {
                public int rowIndex;
                public string key;
            }

            public static void Export()
            {
                Log.Info("Start Export Localization Excel ...");
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                SortedDictionary<Language, SortedDictionary<string, string>> resultDict = new SortedDictionary<Language, SortedDictionary<string, string>>();
                using (FileStream stream = new FileStream(LocalizationExcelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        DataSet dataSet = reader.AsDataSet();
                        foreach (DataTable table in dataSet.Tables)
                        {
                            if (!table.Rows[0][0].ToString().StartsWith("##"))
                            {
                                continue;
                            }
                            int startRowIndex = 0;
                            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
                            {
                                if (table.Rows[rowIndex][0].ToString().StartsWith("##"))
                                {
                                    continue;
                                }
                                startRowIndex = rowIndex;
                                break;
                            }

                            if (startRowIndex == 0)
                            {
                                continue;
                            }
                            
                            //生成key和Language的信息
                            List<KeyTableInfo> keyTableInfos = new List<KeyTableInfo>();
                            List<LanguageTableInfo> languageTableInfos = new List<LanguageTableInfo>();
                            DataRow firstDataRow = table.Rows[0];
                            for (int columnIndex = 1; columnIndex < table.Columns.Count; columnIndex++)
                            {
                                string cellValue = firstDataRow[columnIndex].ToString();
                                if (cellValue.StartsWith("##"))
                                {
                                    continue;
                                }
                                if (cellValue == "key")
                                {
                                    for (int keyRowIndex = startRowIndex; keyRowIndex < table.Rows.Count; keyRowIndex++)
                                    {
                                        keyTableInfos.Add(new KeyTableInfo()
                                        {
                                            key = table.Rows[keyRowIndex][columnIndex].ToString(),
                                            rowIndex = keyRowIndex,
                                        });
                                    }
                                }
                                else
                                {
                                    if (Enum.TryParse(cellValue, out Language language))
                                    {
                                        foreach (var languageTableInfo in languageTableInfos)
                                        {
                                            if (languageTableInfo.language == language)
                                            {
                                                throw new Exception(GetErrorString(table, columnIndex, 0, $"Language {cellValue} is duplicate!"));
                                            }
                                        }
                                        languageTableInfos.Add(new LanguageTableInfo()
                                        {
                                            columnIndex = columnIndex,
                                            language = language,
                                        });
                                    }
                                    else
                                    {
                                        throw new Exception(GetErrorString(table, columnIndex, 0, $"Language {cellValue} is not exit, please get right Language string from GameFramework.Localization.Language.cs!"));
                                    }
                                }
                            }

                            foreach (var keyTableInfo in keyTableInfos)
                            {
                                foreach (var languageTableInfo in languageTableInfos)
                                {
                                    if (!resultDict.TryGetValue(languageTableInfo.language, out var dict))
                                    {
                                        dict = new SortedDictionary<string, string>();
                                        resultDict.Add(languageTableInfo.language, dict);
                                    }

                                    if (dict.ContainsKey(keyTableInfo.key))
                                    {
                                        throw new Exception(GetErrorString(table, languageTableInfo.columnIndex, keyTableInfo.rowIndex, $"Language key {keyTableInfo.key} is duplicate!"));
                                    }
                                    dict.Add(keyTableInfo.key, table.Rows[keyTableInfo.rowIndex][languageTableInfo.columnIndex].ToString());
                                }
                            }
                        }
                    }
                }
                
                bool useJson = Options.Instance.Customs.Contains("Json", StringComparison.OrdinalIgnoreCase);
                //多语言写入配置文件
                foreach (var pair in resultDict)
                {
                    Language language = pair.Key;
                    SortedDictionary<string, string> dict = pair.Value;
                    string resFullPath = Path.GetFullPath($"{LocalizationOutPutDir}/{language.ToString()}");
                    if (!Directory.Exists(resFullPath))
                    {
                        Directory.CreateDirectory(resFullPath);
                    }
                    string jsonFileFullPath = Path.GetFullPath($"{resFullPath}/Localization.json");
                    string bytesFileFullPath = Path.GetFullPath($"{resFullPath}/Localization.bytes");
                    if (useJson)
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        Utf8JsonWriter jsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions()
                        {
                            Indented = true,
                            SkipValidation = false,
                            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        });
                        jsonWriter.WriteStartObject();
                        foreach (var kv in dict)
                        {
                            jsonWriter.WritePropertyName(kv.Key);
                            jsonWriter.WriteStringValue(kv.Value);
                        }
                        jsonWriter.WriteEndObject();
                        jsonWriter.Flush();
                        byte[] bytes = new byte[memoryStream.Length];
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.Read(bytes, 0, bytes.Length);
                        File.WriteAllBytes(jsonFileFullPath, bytes);
                        if (File.Exists(bytesFileFullPath))
                        {
                            File.Delete(bytesFileFullPath);
                        }
                    }
                    else
                    {
                        ByteBuf byteBuf = new ByteBuf();
                        foreach (var kv in dict)
                        {
                            byteBuf.WriteString(kv.Key);
                            byteBuf.WriteString(kv.Value);
                        }
                        File.WriteAllBytes(bytesFileFullPath, byteBuf.CopyData());
                        
                        if (File.Exists(jsonFileFullPath))
                        {
                            File.Delete(jsonFileFullPath);
                        }
                    }
                    Log.Info($"Gen {language} Success!");
                }

                GenerateAssetUtilityCode(useJson? "json" : "bytes");
                GenerateReadyLanguageCode(resultDict.Keys.ToArray());
                Log.Info("Export Localization Excel Success!");
            }

            private static void GenerateAssetUtilityCode(string extensionName)
            {
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine("// This is an automatically generated class by Share.Tool. Please do not modify it.");
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("using GameFramework;");
                stringBuilder.AppendLine("using GameFramework.Localization;");
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("namespace Game");
                stringBuilder.AppendLine("{");
                stringBuilder.AppendLine("    public static partial class AssetUtility");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        public static string GetLocalizationAsset(Language language)");
                stringBuilder.AppendLine("        {");
                stringBuilder.AppendLine("            return Utility.Text.Format(\"Assets/Res/Localization/{0}/Localization.extensionName\", language);".Replace("extensionName", extensionName));
                stringBuilder.AppendLine("        }");
                stringBuilder.AppendLine("    }");
                stringBuilder.AppendLine("}");
                string codeContent = stringBuilder.ToString();
                if (!File.Exists(AssetUtilityCodeFile) || !string.Equals(codeContent, File.ReadAllText(AssetUtilityCodeFile)))
                {
                    File.WriteAllText(AssetUtilityCodeFile, codeContent);
                    Log.Info($"Generate code : {AssetUtilityCodeFile}!");
                }
            }
            
            private static void GenerateReadyLanguageCode(Language[] readyLanguages)
            {
                StringBuilder stringBuilder = new();
                stringBuilder.AppendLine("using GameFramework.Localization;");
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("namespace Game.Editor");
                stringBuilder.AppendLine("{");
                stringBuilder.AppendLine("    public static class LocalizationReadyLanguage");
                stringBuilder.AppendLine("    {");
                stringBuilder.AppendLine("        public static Language[] Languages => new Language[]");
                stringBuilder.AppendLine("        {");
                foreach (Language language in readyLanguages)
                {
                    stringBuilder.AppendLine($"            Language.{language},");
                }
                stringBuilder.AppendLine("        };");
                stringBuilder.AppendLine("    }");
                stringBuilder.AppendLine("}");
                string codeContent = stringBuilder.ToString();
                if (!File.Exists(LocalizationReadyLanguageCodeFile) || !string.Equals(codeContent, File.ReadAllText(LocalizationReadyLanguageCodeFile)))
                {
                    File.WriteAllText(LocalizationReadyLanguageCodeFile, codeContent);
                    Log.Info($"Generate code : {LocalizationReadyLanguageCodeFile}!");
                }
            }

            private static string GetErrorString(DataTable table, int columnIndex, int rowIndex, string errorMsg)
            {
                string ToAlphaString(int column)
                {
                    int h = column / 26;
                    int n = column % 26;
                    return $"{(h > 0 ? ((char)('A' + h - 1)).ToString() : "")}{(char)('A' + n)}";
                }
                
                string error = $@"
=======================================================================
    解析失败!

        文件:        {LocalizationExcelFile}
        错误位置:    sheet:{table.TableName} [{ToAlphaString(columnIndex)}:{rowIndex + 1}] {table.Rows[rowIndex][columnIndex]}
        Err:         {errorMsg}
        字段:        {table.Rows[0][columnIndex]}

=======================================================================
";
                return error;
            }
        }
    }
} 