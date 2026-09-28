#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace OpenGSCore
{
    public sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _buffer;
        private int _index;
        private const int DefaultInitialBufferSize = 256;

        public PooledBufferWriter(int initialCapacity = DefaultInitialBufferSize)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _index = 0;
        }

        public void Advance(int count)
        {
            _index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_index);
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint == 0) sizeHint = 1;
            int availableSpace = _buffer.Length - _index;
            if (availableSpace < sizeHint)
            {
                int newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
                var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
                Array.Copy(_buffer, 0, newBuffer, 0, _index);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = newBuffer;
            }
        }

        public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null!;
            _index = 0;
        }
    }
    public enum AIXJsonType
    {
        Null, Boolean, Number, String, Array, Object
    }

    public readonly struct AIXJsonValue
    {
        public AIXJsonType Type { get; }
        public object? Value { get; }
        public static implicit operator AIXJsonValue(string value) => new(value);
        public static implicit operator AIXJsonValue(bool value) => new(value);
        public static implicit operator AIXJsonValue(int value) => new(value);
        public static implicit operator AIXJsonValue(double value) => new(value);
        public static implicit operator AIXJsonValue(float value) => new(value);
        public static implicit operator AIXJsonValue(long value) => new(value);
        public static implicit operator AIXJsonValue(short value) => new(value);
        public static implicit operator AIXJsonValue(byte value) => new(value);
        public static implicit operator AIXJsonValue(AIXJsonObject value) => new(value);
        public static implicit operator AIXJsonValue(AIXJsonArray value) => new(value);
        public static implicit operator AIXJsonValue(System.DBNull _) => new(null);
        //public static implicit operator AIXJsonValue(object? value) => new(value); // optional
        public AIXJsonValue(object? value)
        {
            Value = value;
            Type = value switch
            {
                null => AIXJsonType.Null,
                bool => AIXJsonType.Boolean,
                double or int or float or long or short or byte => AIXJsonType.Number,
                string => AIXJsonType.String,
                AIXJsonArray => AIXJsonType.Array,
                AIXJsonObject => AIXJsonType.Object,
                _ => throw new ArgumentException($"Unsupported value type: {value.GetType()}")
            };
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            switch (Type)
            {
                case AIXJsonType.Null:
                    writer.WriteNullValue(); break;
                case AIXJsonType.Boolean:
                    writer.WriteBooleanValue((bool)Value!); break;
                case AIXJsonType.Number:
                    switch (Value)
                    {
                        case int i: writer.WriteNumberValue(i); break;
                        case double d: writer.WriteNumberValue(d); break;
                        case float f: writer.WriteNumberValue(f); break;
                        case long l: writer.WriteNumberValue(l); break;
                        default: throw new NotSupportedException();
                    }
                    break;
                case AIXJsonType.String:
                    writer.WriteStringValue((string)Value!); break;
                case AIXJsonType.Array:
                    ((AIXJsonArray)Value!).WriteTo(writer); break;
                case AIXJsonType.Object:
                    ((AIXJsonObject)Value!).WriteTo(writer); break;
            }
        }

        public bool IsNull => Type == AIXJsonType.Null;

        public AIXJsonValue this[int index]
        {
            get
            {
                if (Type == AIXJsonType.Array)
                    return ((AIXJsonArray)Value!)[index];
                throw new InvalidOperationException("Value is not an array");
            }
            set
            {
                if (Type == AIXJsonType.Array)
                    ((AIXJsonArray)Value!)[index] = value;
                else
                    throw new InvalidOperationException("Value is not an array");
            }
        }

    }

    public sealed class AIXJsonArray : List<AIXJsonValue>
    {
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var item in this)
                item.WriteTo(writer);
            writer.WriteEndArray();
        }
    }

    public sealed class AIXJsonObject
    {
        private readonly Dictionary<string, AIXJsonValue> _dict = new();

        public AIXJsonValue this[string key]
        {
            get => _dict.TryGetValue(key, out var val) ? val : new AIXJsonValue(null);
            set => _dict[key] = value;
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (var kv in _dict)
            {
                writer.WritePropertyName(kv.Key);
                kv.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        public override string ToString()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });

            WriteTo(writer);
            writer.Flush();

            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        public void Clear()
        {
            _dict.Clear();
        }
        public void RemoveAll()
        {
            _dict.Clear();
        }
        public bool ContainsKey(string key) => _dict.ContainsKey(key);
    }

    public static class AIXJsonSerializer
    {
        public static byte[] SerializeToUtf8BytesPooled(AIXJsonObject obj)
        {
            using var buffer = new PooledBufferWriter();
            using var writer = new Utf8JsonWriter(buffer);

            obj.WriteTo(writer);
            writer.Flush();

            var span = buffer.WrittenSpan;
            var result = new byte[span.Length];
            span.CopyTo(result);  // ここは外部に渡すためコピーは必要

            return result;
        }
        public static byte[] SerializeToUtf8Bytes(AIXJsonObject obj)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer);

            obj.WriteTo(writer);
            writer.Flush();

            return buffer.WrittenSpan.ToArray();
        }
    }

    public static class AIXJsonObjectExtensions
    {
        /// <summary>
        /// キーが存在する場合は値を返し、存在しない場合はデフォルト値を返す
        /// </summary>
        public static AIXJsonValue GetValueOrDefault(this AIXJsonObject obj, string key, AIXJsonValue defaultValue = default)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (key == null) throw new ArgumentNullException(nameof(key));

            return obj[key].Type == AIXJsonType.Null ? defaultValue : obj[key];
        }

        /// <summary>
        /// 指定キーの値を文字列として取得、存在しない場合は空文字
        /// </summary>
        public static string GetString(this AIXJsonObject obj, string key, string defaultValue = "")
        {
            var val = obj.GetValueOrDefault(key);
            return val.Type == AIXJsonType.String ? (string)val.Value! : defaultValue;
        }

        /// <summary>
        /// 指定キーの値をboolとして取得、存在しない場合はfalse
        /// </summary>
        public static bool GetBool(this AIXJsonObject obj, string key, bool defaultValue = false)
        {
            var val = obj.GetValueOrDefault(key);
            return val.Type == AIXJsonType.Boolean ? (bool)val.Value! : defaultValue;
        }

        /// <summary>
        /// 指定キーの値を数値として取得、存在しない場合は0
        /// </summary>
        public static double GetNumber(this AIXJsonObject obj, string key, double defaultValue = 0)
        {
            var val = obj.GetValueOrDefault(key);
            return val.Type == AIXJsonType.Number ? Convert.ToDouble(val.Value!) : defaultValue;
        }
    }

    public static class AIXJsonArrayExtensions
    {
        /// <summary>
        /// インデックスが範囲外ならデフォルト値を返す
        /// </summary>
        public static AIXJsonValue GetValueOrDefault(this AIXJsonArray arr, int index, AIXJsonValue defaultValue = default)
        {
            if (arr == null) throw new ArgumentNullException(nameof(arr));
            return index >= 0 && index < arr.Count ? arr[index] : defaultValue;
        }

        public static string GetString(this AIXJsonArray arr, int index, string defaultValue = "")
        {
            var val = arr.GetValueOrDefault(index);
            return val.Type == AIXJsonType.String ? (string)val.Value! : defaultValue;
        }

        public static bool GetBool(this AIXJsonArray arr, int index, bool defaultValue = false)
        {
            var val = arr.GetValueOrDefault(index);
            return val.Type == AIXJsonType.Boolean ? (bool)val.Value! : defaultValue;
        }

        public static double GetNumber(this AIXJsonArray arr, int index, double defaultValue = 0)
        {
            var val = arr.GetValueOrDefault(index);
            return val.Type == AIXJsonType.Number ? Convert.ToDouble(val.Value!) : defaultValue;
        }

        public static AIXJsonObject? GetObject(this AIXJsonArray arr, int index)
        {
            var val = arr.GetValueOrDefault(index);
            return val.Type == AIXJsonType.Object ? (AIXJsonObject)val.Value! : null;
        }

        public static AIXJsonArray? GetArray(this AIXJsonArray arr, int index)
        {
            var val = arr.GetValueOrDefault(index);
            return val.Type == AIXJsonType.Array ? (AIXJsonArray)val.Value! : null;
        }
    }


}

