using MemoryPack;

/// <summary>
/// A wrapper packet used when the server explicitly rejects a client's packet.
/// </summary>
/// <typeparam name="T">The original packet type that was rejected.</typeparam>
[MemoryPackable]
public partial struct RejectionPacket<T> : IPacket
    where T : IPacket, new()
{
    public T Payload;
    public string rejectionReason;
}