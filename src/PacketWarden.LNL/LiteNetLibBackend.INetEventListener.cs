using System.Net;
using System.Net.Sockets;
using LiteNetLib;

public partial class LNLBackend
{
    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        UnityEngine.Debug.Log("[CLIENT] We connected to " + peer);
        OnNetworkCreated?.Invoke();

        OnPeerConnected?.Invoke(peer.Id);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        UnityEngine.Debug.Log("[CLIENT] We received error " + socketErrorCode);
    }

    void INetEventListener.OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channelNumber,
        DeliveryMethod deliveryMethod
    )
    {
        ushort packetId = reader.GetUShort();
        var handler = _packetHandlers[packetId];
        handler?.Invoke(reader, peer.Id);
    }

    void INetEventListener.OnNetworkReceiveUnconnected(
        IPEndPoint remoteEndPoint,
        NetPacketReader reader,
        UnconnectedMessageType messageType
    ) { }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.Accept();
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        OnPeerDisconnected?.Invoke(peer.Id);
    }
}
