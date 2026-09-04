using System;

/// <summary>
/// Defines who has permission to send and receive this packet type.
/// </summary>
public enum PacketAuthority
{
    Anyone,
    Admin,
    ServerOnly,
}

/// <summary>
/// The core abstract manager for a specific network packet. <br/>
/// Handles rate limiting, authority validation, sanitization, server-side broadcasting, <br/>
/// and client-side prediction (optimistic execution). <br/>
/// </summary>
/// <typeparam name="T">The packet struct managed by this warden.</typeparam>
public abstract partial class PacketWarden<T> : IDisposable
    where T : IPacket, new()
{
    public PacketWarden(INetworkTransport network)
    {
        Network = network;
        Network.OnNetworkCreated += RegisterPacket;
        Network.OnNetworkDestroyed += DeregisterPacket;

        RegisterPacket();
    }

    public virtual void Dispose()
    {
        if (Network == null)
            return;

        Network.OnNetworkCreated -= RegisterPacket;
        Network.OnNetworkDestroyed -= DeregisterPacket;

        DeregisterPacket();
    }

    protected INetworkTransport Network { get; private set; }

    public bool IsRegistered { get; private set; } = false;

    private readonly TokenBucketRateLimiter<int> _serverRateLimiter = new();

    #region Configuration
    // Rate Limiting runs on the main thread
    // this means that if network backend gets a packet that's out of order (e.g. in ReliableOrdered channel)
    // it will await until the previous packets come in.
    // This will ultimately result in packets being incorrectly flagged as coming too fast, and be will rejected.
    // in most cases this is fine.
    /// <summary>Defines the rate limiting policy for this packet on the server side. Defaults to Disabled.</summary>
    protected virtual RateLimitConfig ServerRateLimit => RateLimitPresets.Disabled;

    protected virtual DeliveryType DeliveryType => DeliveryType.ReliableOrdered;
    protected virtual PacketAuthority Authority => PacketAuthority.Anyone;

    protected virtual bool ShouldLog => true;
    protected virtual bool ShouldNotifyAboutRejection => false;
    #endregion
}
