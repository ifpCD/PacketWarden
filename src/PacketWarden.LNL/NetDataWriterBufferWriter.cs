using System;
using System.Buffers;
using LiteNetLib.Utils;
using MemoryPack;

internal class NetDataWriterBufferWriter : IBufferWriter<byte>
{
    public NetDataWriter Writer;

    public void Advance(int count)
    {
        Writer.SetPosition(Writer.Length + count);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        int size = sizeHint > 0 ? sizeHint : 256;
        Writer.EnsureFit(size);
        return Writer.Data.AsMemory(Writer.Length);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        int size = sizeHint > 0 ? sizeHint : 256;
        Writer.EnsureFit(size);
        return Writer.Data.AsSpan(Writer.Length);
    }
}
