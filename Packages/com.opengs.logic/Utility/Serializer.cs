using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OpenGSCore.Utility
{
    public static class Serializer
    {
        private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = false };
        private static readonly MessagePackSerializerOptions msgPackOptions = MessagePackSerializerOptions.Standard;

        public static string ToJson<T>(T obj) => JsonSerializer.Serialize(obj, jsonOptions);
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, jsonOptions);

        public static byte[] ToMsgPack<T>(T obj) => MessagePackSerializer.Serialize(obj, msgPackOptions);
        public static T FromMsgPack<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data, msgPackOptions);


    }
}
