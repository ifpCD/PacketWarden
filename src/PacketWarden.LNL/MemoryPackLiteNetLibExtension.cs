using System;
using LiteNetLib.Utils;
using MemoryPack;

public static class MemoryPackLiteNetExtensions
{
    [ThreadStatic]
    private static NetDataWriterBufferWriter _cachedBufferWriter;

    /// <summary>
    /// Serializes a MemoryPackable object directly into the NetDataWriter.
    /// </summary>
    public static void PutPackable<T>(this NetDataWriter writer, in T value)
    {
        _cachedBufferWriter ??= new NetDataWriterBufferWriter();

        _cachedBufferWriter.Writer = writer;

        try
        {
            MemoryPackSerializer.Serialize(_cachedBufferWriter, in value);
        }
        finally
        {
            _cachedBufferWriter.Writer = null;
        }
    }

    /// <summary>
    /// Deserializes a MemoryPackable object directly from the NetDataReader.
    /// </summary>
    public static T GetPackable<T>(this NetDataReader reader)
    {
        var span = reader.GetRemainingBytesSpan();
        T value = default;

        int consumed = MemoryPackSerializer.Deserialize(span, ref value);

        reader.SkipBytes(consumed);
        return value;
    }

    /// <summary>
    /// Deserializes into an existing object instance to reuse memory.
    /// </summary>
    public static void GetPackableInto<T>(this NetDataReader reader, ref T value)
    {
        var span = reader.GetRemainingBytesSpan();
        int consumed = MemoryPackSerializer.Deserialize(span, ref value);
        reader.SkipBytes(consumed);
    }
}
