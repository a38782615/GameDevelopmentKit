﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ET
{
    public static partial class Proto2CS
    {
        public static class Proto2CS_UGF
        {
            private static readonly List<OpcodeInfo> msgOpcode = new List<OpcodeInfo>();
            private static string csName;
            private static List<string> csOutDirs;
            private static int startOpcode;
            private static StringBuilder sb;

            public static void Start(string codeName, List<string> outDirs, int opcode, string nameSpace)
            {
                csName = codeName;
                csOutDirs = outDirs;
                startOpcode = opcode;
                
                sb = new StringBuilder();
                sb.Append("// This is an automatically generated class by Share.Tool. Please do not modify it.\n");
                sb.Append("\n");
                sb.Append("using ProtoBuf;\n");
                sb.Append("using System;\n");
                sb.Append("using System.Collections.Generic;\n");
                sb.Append("\n");
                sb.Append($"namespace {nameSpace}\n");
                sb.Append("{\n");
            }

            public static void Stop()
            {
                sb.Append("}\n");
                foreach (var csOutDir in csOutDirs)
                {
                    GenerateCS(sb, csOutDir, csName);
                }
            }

            public static void Proto2CS(string protoFile)
            {
                string s = File.ReadAllText(protoFile);
                
                bool isMsgStart = false;
                StringBuilder disposeSb = new StringBuilder();
                foreach (string line in s.Split('\n'))
                {
                    string newline = line.Trim();
                    
                    if (newline == "")
                    {
                        continue;
                    }

                    if (newline.StartsWith("//"))
                    {
                        sb.Append($"{newline}\n");
                        continue;
                    }

                    if (newline.StartsWith("message"))
                    {
                        string parentClass = "";
                        isMsgStart = true;

                        disposeSb.Clear();
                        disposeSb.Append($"\t\tpublic override void Clear()\n");
                        disposeSb.Append("\t\t{\n");

                        string msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                        string[] ss = newline.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                        if (ss.Length == 2)
                        {
                            parentClass = ss[1].Trim();
                        }
                        
                        sb.Append($"\t[Serializable, ProtoContract(Name = @\"{msgName}\")]\n");
                        sb.Append($"\t//protofile : {protoFile.Replace("\\", "/").Split("/")[^2]}/{Path.GetFileName(protoFile)}\n");
                        sb.Append($"\tpublic partial class {msgName}");
                        if (string.IsNullOrEmpty(parentClass))
                        {
                            if (msgName.StartsWith("CS", StringComparison.OrdinalIgnoreCase))
                            {
                                sb.Append(" : CSPacketBase\n");
                            }
                            else if (msgName.StartsWith("SC", StringComparison.OrdinalIgnoreCase))
                            {
                                sb.Append(" : SCPacketBase\n");
                            }
                            else
                            {
                                throw new Exception("\n");
                            }
                        }
                        else
                        {
                            sb.Append($" : {parentClass}\n");
                        }
                        
                        continue;
                    }

                    if (isMsgStart)
                    {
                        if (newline == "{")
                        {
                            sb.Append("\t{\n");
                            sb.Append($"\t\tpublic override int Id => {++startOpcode};\n");
                            continue;
                        }

                        if (newline == "}")
                        {
                            isMsgStart = false;
                            disposeSb.Append("\t\t}\n");
                            sb.Append(disposeSb.ToString());
                            disposeSb.Clear();
                            sb.Append("\t}\n\n");
                            continue;
                        }

                        if (newline.Trim().StartsWith("//"))
                        {
                            sb.Append($"{newline}\n");
                            continue;
                        }

                        if (newline.Trim() != "" && newline != "}")
                        {
                            if (newline.StartsWith("map<"))
                            {
                                Map(sb, newline, disposeSb);
                            }
                            else if (newline.StartsWith("repeated"))
                            {
                                Repeated(sb, newline, disposeSb);
                            }
                            else
                            {
                                Members(sb, newline, disposeSb);
                            }
                        }
                    }
                }
            }

            private static void GenerateCS(StringBuilder sb, string path, string csName)
            {
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string csPath = $"{path}/{csName}.cs";
                using FileStream txt = new FileStream(csPath, FileMode.Create, FileAccess.ReadWrite);
                using StreamWriter sw = new StreamWriter(txt);
                sw.Write(sb.ToString().Replace("\t", "    "));

                Log.Info($"proto2cs file : {csPath}");
            }

            private static void Map(StringBuilder sb, string newline, StringBuilder disposeSb)
            {
                try
                {
                    int start = newline.IndexOf("<") + 1;
                    int end = newline.IndexOf(">");
                    string types = newline.Substring(start, end - start);
                    string[] ss = types.Split(",");
                    string keyType = ConvertType(ss[0].Trim());
                    string valueType = ConvertType(ss[1].Trim());
                    string tail = newline.Substring(end + 1);
                    ss = tail.Trim().Replace(";", "").Split(" ");
                    string v = ss[0];
                    string n = ss[2];
                
                    sb.Append($"\t\t[ProtoMember({n})]\n");
                    sb.Append($"\t\tpublic Dictionary<{keyType}, {valueType}> {v} {{ get; set; }} = new Dictionary<{keyType}, {valueType}>();\n");

                    disposeSb.Append($"\t\t\tthis.{v}.Clear();\n");
                }
                catch (Exception)
                {
                    Log.Error($"ErrorLine => \"{csName}\" : \"{newline}\"\n");
                    throw;
                }
            }

            private static void Repeated(StringBuilder sb, string newline, StringBuilder disposeSb)
            {
                try
                {
                    int index = newline.IndexOf(";");
                    newline = newline.Remove(index);
                    string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                    string type = ss[1];
                    type = ConvertType(type);
                    string name = ss[2];
                    int n = int.Parse(ss[4]);

                    sb.Append($"\t\t[ProtoMember({n})]\n");
                    sb.Append($"\t\tpublic List<{type}> {name} {{ get; set; }} = new List<{type}>();\n");

                    disposeSb.Append($"\t\t\tthis.{name}.Clear();\n");
                }
                catch (Exception)
                {
                    Log.Error($"ErrorLine => \"{csName}\" : \"{newline}\"\n");
                    throw;
                }
            }

            private static string ConvertType(string type)
            {
                string typeCs = "";
                switch (type)
                {
                    case "int16":
                        typeCs = "short";
                        break;
                    case "int32":
                        typeCs = "int";
                        break;
                    case "bytes":
                        typeCs = "byte[]";
                        break;
                    case "uint32":
                        typeCs = "uint";
                        break;
                    case "long":
                        typeCs = "long";
                        break;
                    case "int64":
                        typeCs = "long";
                        break;
                    case "uint64":
                        typeCs = "ulong";
                        break;
                    case "uint16":
                        typeCs = "ushort";
                        break;
                    default:
                        typeCs = type;
                        break;
                }

                return typeCs;
            }

            private static void Members(StringBuilder sb, string newline, StringBuilder disposeSb)
            {
                try
                {
                    int index = newline.IndexOf(";");
                    newline = newline.Remove(index);
                    string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                    string type = ss[0];
                    string name = ss[1];
                    int n = int.Parse(ss[3]);
                    string typeCs = ConvertType(type);

                    sb.Append($"\t\t[ProtoMember({n})]\n");
                    sb.Append($"\t\tpublic {typeCs} {name} {{ get; set; }}\n");

                    switch (typeCs)
                    {
                        case "bytes":
                        {
                            break;
                        }
                        default:
                            disposeSb.Append($"\t\t\tthis.{name} = default;\n");
                            break;
                    }
                }
                catch (Exception)
                {
                    Log.Error($"ErrorLine => \"{csName}\" : \"{newline}\"\n");
                    throw;
                }
            }
        }
    }
}