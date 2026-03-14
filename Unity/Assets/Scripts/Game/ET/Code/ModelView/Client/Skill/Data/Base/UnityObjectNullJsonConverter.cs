using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ET.Client
{
    [EnableClass]
    public class UnityObjectNullJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

#if UNITY_EDITOR
            if (token.Type != JTokenType.Null)
            {
                SkillDiagFileLogger.Log($"[DiagSkillDataConverter] ignore UnityObject type={objectType.Name} token={Truncate(token.ToString(Formatting.None), 200)}");
            }
#endif

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteNull();
        }

#if UNITY_EDITOR
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength) + "...";
        }
#endif
    }
}
