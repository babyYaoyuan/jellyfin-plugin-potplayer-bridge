using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Net;

namespace Jellyfin.Plugin.PotPlayerBridge.Services;

/// <summary>
/// In-memory store for short-lived, item-scoped media stream tickets.
/// </summary>
public sealed class StreamTicketStore
{
    private readonly ConcurrentDictionary<string, StreamTicket> _tickets =
        new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly int _maximumTicketCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTicketStore"/> class.
    /// </summary>
    public StreamTicketStore(TimeProvider timeProvider, int maximumTicketCount = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumTicketCount);
        _timeProvider = timeProvider;
        _maximumTicketCount = maximumTicketCount;
    }

    /// <summary>
    /// Creates a ticket for one existing Jellyfin library file.
    /// </summary>
    public StreamTicket Create(
        Guid itemId,
        string path,
        string contentType,
        TimeSpan lifetime,
        IPAddress? remoteAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        RemoveExpired();
        TrimToCapacity();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var ticket = new StreamTicket(
            token,
            itemId,
            path,
            System.IO.Path.GetFileName(path),
            contentType,
            _timeProvider.GetUtcNow().Add(lifetime),
            NormalizeAddress(remoteAddress));
        _tickets[token] = ticket;
        return ticket;
    }

    /// <summary>
    /// Looks up a ticket and enforces its expiration and optional address binding.
    /// </summary>
    public bool TryGet(string token, IPAddress? remoteAddress, out StreamTicket? ticket)
    {
        ticket = null;
        if (string.IsNullOrWhiteSpace(token) || !_tickets.TryGetValue(token, out var candidate))
        {
            return false;
        }

        if (candidate.ExpiresAtUtc <= _timeProvider.GetUtcNow())
        {
            _tickets.TryRemove(token, out _);
            return false;
        }

        if (candidate.RemoteAddress is not null &&
            !string.Equals(candidate.RemoteAddress, NormalizeAddress(remoteAddress), StringComparison.Ordinal))
        {
            return false;
        }

        ticket = candidate;
        return true;
    }

    private static string? NormalizeAddress(IPAddress? address)
    {
        if (address?.IsIPv4MappedToIPv6 == true)
        {
            address = address.MapToIPv4();
        }

        return address?.ToString();
    }

    private void RemoveExpired()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var entry in _tickets)
        {
            if (entry.Value.ExpiresAtUtc <= now)
            {
                _tickets.TryRemove(entry.Key, out _);
            }
        }
    }

    private void TrimToCapacity()
    {
        while (_tickets.Count >= _maximumTicketCount)
        {
            var oldest = _tickets.Values.MinBy(value => value.ExpiresAtUtc);
            if (oldest is null || !_tickets.TryRemove(oldest.Token, out _))
            {
                break;
            }
        }
    }
}
