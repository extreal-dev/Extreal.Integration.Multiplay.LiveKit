using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var x = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var y = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.EndArray)
            {
                throw new JsonException();
            }

            return new Vector2(x, y);
        }


        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteEndArray();
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var x = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var y = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var z = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.EndArray)
            {
                throw new JsonException();
            }

            return new Vector3(x, y, z);
        }


        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var x = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var y = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var z = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.Float)
            {
                throw new JsonException();
            }
            var w = float.Parse(reader.Value.ToString());

            reader.Read();
            if (reader.TokenType != JsonToken.EndArray)
            {
                throw new JsonException();
            }

            return new Quaternion(x, y, z, w);
        }


        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteValue(value.w);
            writer.WriteEndArray();
        }
    }
}
