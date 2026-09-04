using System;
using System.Collections.Generic;
using static INetworkTransport;

public class LocalBackend : INetworkTransport
{
    public bool IsServer => true;
    public bool IsClient => false;
    public bool IsHeadless => false;
    public bool IsOnline => false;

    public int NetId => LocalPeerId;

    public Action OnNetworkCreated { get; set; }
    public Action OnNetworkDestroyed { get; set; }

    public Action<int> OnPeerConnected { get; set; }
    public Action<int> OnPeerDisconnected { get; set; }

    public void Dispose() { }

    private readonly Dictionary<Type, Delegate> _handlers = new();

    public void RegisterPacket<T>(NetPacketHandler<T> onReceive)
        where T : IPacket
    {
        _handlers[typeof(T)] = onReceive;
    }

    public void DeregisterPacket<T>()
        where T : IPacket
    {
        _handlers.Remove(typeof(T));
    }

    public void SendData<T>(ref T packet, DeliveryType method)
        where T : IPacket
    {
        if (_handlers.TryGetValue(typeof(T), out var del))
        {
            NetPacketHandler<T> handler = (NetPacketHandler<T>)del;
            handler(ref packet, 0);
        }
    }

    public void SendDataToPeer<T>(ref T packet, DeliveryType method, int id)
        where T : IPacket { }

    public void DisconnectPeer(int peerId) { }
}
