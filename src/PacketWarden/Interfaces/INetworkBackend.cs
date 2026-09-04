using System;

public enum DeliveryType : byte
{
    ReliableUnordered = 0,
    Sequenced = 1,
    ReliableOrdered = 2,
    ReliableSequenced = 3,
    Unreliable = 4,
}

public enum LocalPeerType
{
    Client,
    Server,
    HeadlessServer,
}

public interface INetworkTransport : IDisposable
{
    public const int LocalPeerId = -1;

    bool IsServer { get; }
    bool IsClient { get; }
    bool IsHeadless { get; }
    bool IsOnline { get; }

    int NetId { get; }

    Action OnNetworkCreated { get; set; }
    Action OnNetworkDestroyed { get; set; }
    Action<int> OnPeerConnected { get; set; }
    Action<int> OnPeerDisconnected { get; set; }

    public delegate void NetPacketHandler<T>(ref T packet, int peerId);

    void RegisterPacket<T>(NetPacketHandler<T> onReceive)
        where T : IPacket;

    void DeregisterPacket<T>()
        where T : IPacket;

    void SendData<T>(ref T packet, DeliveryType method)
        where T : IPacket;

    void SendDataToPeer<T>(ref T packet, DeliveryType method, int peerId)
        where T : IPacket;

    void DisconnectPeer(int peerId);
}
