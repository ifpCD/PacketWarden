using System;

public abstract partial class PacketWarden<T> : IDisposable
    where T : IPacket, new()
{
    protected virtual void WhenClientReceivesPacket(ref T packet, int peerId)
    {
#if UNITY_EDITOR || DEBUG
        if (ShouldLog)
            UnityEngine.Debug.Log(
                $"[PacketWarden] Receiving {typeof(T).Name} from Server at {UnityEngine.Time.time}"
            );
#endif
        Apply(ref packet, peerId);
    }

    protected void WhenClientReceivesRejection(ref RejectionPacket<T> rejectedPacket, int peerId)
    {
        if (ShouldLog)
        {
            UnityEngine.Debug.Log($"Server Rejected {GetType().Name}");
            if (!string.IsNullOrEmpty(rejectedPacket.rejectionReason))
                UnityEngine.Debug.Log(rejectedPacket.rejectionReason);
        }

        if (ShouldNotifyAboutRejection && !string.IsNullOrEmpty(rejectedPacket.rejectionReason))
        {
            UnityEngine.Debug.Log(rejectedPacket.rejectionReason);
        }

        WhenRejected(ref rejectedPacket.Payload, peerId);
    }

    /// <summary>
    /// Fallback logic invoked on the client if the server rejects this packet. <br/>
    /// Used to rollback state changes made in <see cref="ApplyOptimistically"/>.
    /// </summary>
    protected virtual void WhenRejected(ref T packet, int peerId) { }
}
