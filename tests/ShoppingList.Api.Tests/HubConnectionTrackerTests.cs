using ShoppingList.Api.Services;
using Xunit;

namespace ShoppingList.Api.Tests;

public class HubConnectionTrackerTests
{
    [Fact]
    public void EvictUserFromList_finds_only_matching_connections()
    {
        var tracker = new HubConnectionTracker();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        tracker.TrackJoin("conn-a", userId, listId);
        tracker.TrackJoin("conn-b", otherUserId, listId);
        tracker.TrackJoin("conn-c", userId, Guid.NewGuid());

        var matches = tracker.GetConnectionIdsForUserInList(userId, listId);

        Assert.Single(matches);
        Assert.Equal("conn-a", matches[0]);
    }

    [Fact]
    public void Disconnect_removes_connection_tracking()
    {
        var tracker = new HubConnectionTracker();
        var listId = Guid.NewGuid();

        tracker.TrackJoin("conn-a", Guid.NewGuid(), listId);
        tracker.Disconnect("conn-a");

        Assert.Empty(tracker.GetConnectionIdsInList(listId));
    }
}
