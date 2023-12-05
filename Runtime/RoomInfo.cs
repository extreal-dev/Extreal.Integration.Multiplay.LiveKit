using System;
using System.Text.Json.Serialization;
using UnityEngine;

namespace Extreal.Integration.Multiplay.LiveKit
{
    public class RoomInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
