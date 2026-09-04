using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using static INetworkTransport;

public partial class LNLBackend : INetworkTransport, IDisposable, INetEventListener
{
    public bool IsServer => false;
    public bool IsClient => false;
    public bool IsHeadless => false;
    public bool IsOnline => true;

    public int NetId => 0;

    public Action OnNetworkCreated { get; set; }
    public Action OnNetworkDestroyed { get; set; }

    public Action<int> OnPeerConnected { get; set; }
    public Action<int> OnPeerDisconnected { get; set; }

    readonly NetManager _netManager;
    readonly NetDataWriter _dataWriter = new();

    NetPeer ServerConnection =>
        IsClient && _netManager?.FirstPeer != null ? _netManager.FirstPeer : null;

    public LNLBackend()
    {
        _netManager = new(this);
    }

    public void Host()
    {
        _netManager.Start();
        OnNetworkCreated?.Invoke();
    }

    public void Connect(IPEndPoint endPoint)
    {
        _netManager.Connect(endPoint, "PacketWarden");
    }

    public void Dispose()
    {
        _netManager?.Stop();
    }

    // Map ushort ID -> Deserialization Action
    // csharpier-ignore
    private readonly Action<NetPacketReader, int>[] _packetHandlers = new Action<NetPacketReader, int>[ushort.MaxValue];
    private ushort _nextPacketId = 1;
    private readonly Dictionary<Type, ushort> _packetTypeToId = new();

    public void RegisterPacket<T>(NetPacketHandler<T> onReceive)
        where T : IPacket
    {
        ushort id = _nextPacketId++;
        _packetTypeToId[typeof(T)] = id;

        _packetHandlers[id] = (reader, peerId) =>
        {
            var packet = reader.GetPackable<T>();
            onReceive(ref packet, peerId);
        };
    }

    public void DeregisterPacket<T>()
        where T : IPacket
    {
        if (!_packetTypeToId.TryGetValue(typeof(T), out var id))
            return;

        _packetHandlers[id] = null;
        _packetTypeToId.Remove(typeof(T));
    }

    public void DisconnectPeer(int peerId)
    {
        if (!IsServer)
            return;

        _netManager.TryGetPeerById(peerId, out var peer);
        if (peer == null)
            return;

        peer.Disconnect();
    }

    public void SendData<T>(ref T packet, DeliveryType method)
        where T : IPacket
    {
        if (IsClient && ServerConnection == null)
            return;

        ResetAndWrite(ref packet);

        if (IsClient)
            ServerConnection.Send(_dataWriter.AsReadOnlySpan(), (DeliveryMethod)method);
        else
            _netManager.SendToAll(_dataWriter.AsReadOnlySpan(), (DeliveryMethod)method);
    }

    public void SendDataToPeer<T>(ref T packet, DeliveryType method, int id)
        where T : IPacket
    {
        _netManager.TryGetPeerById(id, out var peer);
        if (peer == null)
            return;

        ResetAndWrite(ref packet);

        peer.Send(_dataWriter.AsReadOnlySpan(), (DeliveryMethod)method);
    }

    void ResetAndWrite<T>(ref T packet)
    {
        _dataWriter.Reset();
        _dataWriter.Put(_packetTypeToId[typeof(T)]);
        _dataWriter.PutPackable(packet);
    }
}
