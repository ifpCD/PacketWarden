using System;

public abstract partial class PacketWarden<T> : IDisposable
    where T : IPacket, new()
{
    #region Registration
    void RegisterPacket()
    {
        UnityEngine.Debug.Log($"Registering {typeof(T).Name}");

        Network.RegisterPacket<T>(WhenReceivedInternal);
        Network.RegisterPacket<RejectionPacket<T>>(WhenRejectionReceivedInternal);

        IsRegistered = true;
    }

    void DeregisterPacket()
    {
        try
        {
            _serverRateLimiter.Clear();
            Network.DeregisterPacket<T>();
            Network.DeregisterPacket<RejectionPacket<T>>();
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Packet Unregistration Failed: {ex}");
        }
        finally
        {
            IsRegistered = false;
        }
    }
    #endregion

    #region Receiving
    void WhenReceivedInternal(ref T packet, int peerId)
    {
        if (Network.IsServer)
            WhenServerReceivesPacket(ref packet, peerId);
        else
            WhenClientReceivesPacket(ref packet, peerId);
    }

    void WhenRejectionReceivedInternal(ref RejectionPacket<T> packet, int peerId)
    {
        if (Network.IsClient)
            WhenClientReceivesRejection(ref packet, peerId);
    }
    #endregion

    #region Sending
    /// <summary>
    /// The entry point for sending a packet. <br/>
    /// Dispatches the packet to the network.<br/>
    /// Clients send to the server; the server processes and broadcasts.
    /// </summary>
    /// <param name="packet">The packet payload to send.</param>
    /// <param name="targetPeerId">If provided (and invoked by the server), the packet is sent only to this specific peer.</param>
    protected void DispatchPacket(ref T packet, int? targetPeerId = null)
    {
        if (Network == null)
            return;

        if (!Network.IsHeadless && IsUnauthorized(INetworkTransport.LocalPeerId))
            return;

#if UNITY_EDITOR || DEBUG
        if (ShouldLog)
            UnityEngine.Debug.Log(
                $"[PacketWarden] Sending {typeof(T).Name} at {UnityEngine.Time.time}"
            );
#endif

        ApplyOptimistically(ref packet);

        if (Network.IsClient)
        {
            Network.SendData(ref packet, DeliveryType);
            return;
        }

        // SERVER SIDE LOGIC
        // if we are the server and we are not sending the packet to anyone specifically
        if (targetPeerId == null)
        {
            ProcessApprovedPacket(ref packet, INetworkTransport.LocalPeerId);
        }
        else
        {
            // tldr - if the server is sending a packet to a specific peer, we do not apply it internally, we just send it to the peer.
            MutateApprovedPacket(ref packet, targetPeerId.Value);
            if (targetPeerId.Value != INetworkTransport.LocalPeerId)
            {
                Network.SendDataToPeer(ref packet, DeliveryType, targetPeerId.Value);
            }
            // if the server is sending a packet to itself, we just apply it internally.
            else
            {
                Apply(ref packet, INetworkTransport.LocalPeerId);
            }
        }
    }
    #endregion

    #region Application Pipeline
    /// <summary>
    /// Called instantly upon dispatching the packet for Client-Side Prediction. <br/>
    /// Mutate local state here before the server confirms it. <br/>
    /// <b>WARNING: This should really only be used for SFX/VFX.</b>
    /// </summary>
    protected virtual void ApplyOptimistically(ref T packet) { }

    /// <summary>
    /// The core execution logic for this packet type. <br/>
    /// Executed locally on the server immediately, and on clients once received from the server.
    /// </summary>
    /// <param name="packet">The deserialized packet payload.</param>
    /// <param name="peerId">The ID of the peer who sent it.</param>
    protected abstract void Apply(ref T packet, int peerId);
    #endregion
}
