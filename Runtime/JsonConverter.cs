using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Messaging
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var x = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var y = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new Vector2(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteEndArray();
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var x = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var y = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var z = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new Vector3(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteEndArray();
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var x = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var y = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var z = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }
            var w = reader.GetSingle();

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new Quaternion(x, y, z, w);
        }

        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteNumberValue(value.w);
            writer.WriteEndArray();
        }
    }
}
