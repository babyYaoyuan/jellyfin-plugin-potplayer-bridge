using System.Net;
using Jellyfin.Plugin.PotPlayerBridge.Services;
using Xunit;

namespace Jellyfin.Plugin.PotPlayerBridge.Tests;

public sealed class StreamTicketStoreTests
{
    [Fact]
    public void TicketIsScopedToCreatingAddress()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero));
        var store = new StreamTicketStore(clock);
        var ticket = store.Create(
            Guid.NewGuid(),
            "/media/movie.mp4",
            "video/mp4",
            TimeSpan.FromHours(1),
            IPAddress.Parse("192.0.2.10"));

        Assert.True(store.TryGet(ticket.Token, IPAddress.Parse("192.0.2.10"), out var resolved));
        Assert.Equal(ticket, resolved);
        Assert.False(store.TryGet(ticket.Token, IPAddress.Parse("192.0.2.11"), out _));
    }

    [Fact]
    public void ExpiredTicketIsRejected()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero));
        var store = new StreamTicketStore(clock);
        var ticket = store.Create(
            Guid.NewGuid(),
            "/media/movie.mkv",
            "video/x-matroska",
            TimeSpan.FromMinutes(5),
            null);

        clock.Advance(TimeSpan.FromMinutes(6));

        Assert.False(store.TryGet(ticket.Token, null, out _));
    }

    [Fact]
    public void TokensHaveTwoHundredFiftySixBitsOfRandomMaterial()
    {
        var store = new StreamTicketStore(TimeProvider.System);
        var first = store.Create(Guid.NewGuid(), "/media/a.mp4", "video/mp4", TimeSpan.FromMinutes(5), null);
        var second = store.Create(Guid.NewGuid(), "/media/b.mp4", "video/mp4", TimeSpan.FromMinutes(5), null);

        Assert.Equal(64, first.Token.Length);
        Assert.NotEqual(first.Token, second.Token);
    }

    [Fact]
    public void OldestTicketIsEvictedWhenCapacityIsReached()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 8, 22, 0, 0, 0, TimeSpan.Zero));
        var store = new StreamTicketStore(clock, maximumTicketCount: 2);
        var first = store.Create(Guid.NewGuid(), "/media/a.mp4", "video/mp4", TimeSpan.FromHours(1), null);

        clock.Advance(TimeSpan.FromSeconds(1));
        var second = store.Create(Guid.NewGuid(), "/media/b.mp4", "video/mp4", TimeSpan.FromHours(1), null);
        clock.Advance(TimeSpan.FromSeconds(1));
        var third = store.Create(Guid.NewGuid(), "/media/c.mp4", "video/mp4", TimeSpan.FromHours(1), null);

        Assert.False(store.TryGet(first.Token, null, out _));
        Assert.True(store.TryGet(second.Token, null, out _));
        Assert.True(store.TryGet(third.Token, null, out _));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public ManualTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan value)
        {
            _now = _now.Add(value);
        }
    }
}
