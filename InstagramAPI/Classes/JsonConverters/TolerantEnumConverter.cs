using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InstagramAPI.Classes.JsonConverters
{
    /// <summary>
    /// Tolerant Enum converter. Based on code in the StackOverflow post below, but adds EnumMember attribute support.
    /// http://stackoverflow.com/questions/22752075/how-can-i-ignore-unknown-enum-values-during-json-deserialization
    /// </summary>
    public class TolerantEnumConverter : JsonConverter
    {
        [ThreadStatic]
        private static Dictionary<Type, Dictionary<string, object>> _fromValueMap; // string representation to Enum value map

        [ThreadStatic]
        private static Dictionary<Type, Dictionary<object, string>> _toValueMap; // Enum value to string map

        public string UnknownValue { get; set; } = "Unknown";

        public override bool CanConvert(Type objectType)
        {
            Type type = IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType;
            return type.IsEnum;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isNullable = IsNullableType(objectType);
            Type enumType = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;

            InitMap(enumType);

            if (reader.TokenType == JsonToken.String)
            {
                string enumText = reader.Value.ToString();

                object val = FromValue(enumType, enumText);

                if (val != null)
                    return val;
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                int enumVal = Convert.ToInt32(reader.Value);
                int[] values = (int[])Enum.GetValues(enumType);
                if (values.Contains(enumVal))
                {
                    return Enum.Parse(enumType, enumVal.ToString());
                }
            }

            if (!isNullable)
            {
                string[] names = Enum.GetNames(enumType);

                string unknownName = names
                    .FirstOrDefault(n => string.Equals(n, UnknownValue, StringComparison.OrdinalIgnoreCase));

                if (unknownName == null)
                {
                    throw new JsonSerializationException($"Unable to parse '{reader.Value}' to enum {enumType}. Consider adding Unknown as fail-back value.");
                }

                return Enum.Parse(enumType, unknownName);
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type enumType = value.GetType();

            InitMap(enumType);

            string val = ToValue(enumType, value);

            writer.WriteValue(val);
        }

        #region Private methods
        private bool IsNullableType(Type t)
        {
            return (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        private void InitMap(Type enumType)
        {
            if (_fromValueMap == null)
                _fromValueMap = new Dictionary<Type, Dictionary<string, object>>();

            if (_toValueMap == null)
                _toValueMap = new Dictionary<Type, Dictionary<object, string>>();

            if (_fromValueMap.ContainsKey(enumType))
                return; // already initialized

            Dictionary<string, object> fromMap = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<object, string> toMap = new Dictionary<object, string>();

            FieldInfo[] fields = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                string name = field.Name;
                object enumValue = Enum.Parse(enumType, name);

                // use EnumMember attribute if exists
                EnumMemberAttribute enumMemberAttrbiute = field.GetCustomAttribute<EnumMemberAttribute>();

                if (enumMemberAttrbiute != null)
                {
                    string enumMemberValue = enumMemberAttrbiute.Value;

                    fromMap[enumMemberValue] = enumValue;
                    toMap[enumValue] = enumMemberValue;
                }
                else
                {
                    toMap[enumValue] = name;
                }

                fromMap[name] = enumValue;
            }

            _fromValueMap[enumType] = fromMap;
            _toValueMap[enumType] = toMap;
        }

        private string ToValue(Type enumType, object obj)
        {
            Dictionary<object, string> map = _toValueMap[enumType];

            return map[obj];
        }

        private object FromValue(Type enumType, string value)
        {
            Dictionary<string, object> map = _fromValueMap[enumType];

            if (!map.ContainsKey(value))
                return null;

            return map[value];
        }
        #endregion
    }
}
