using System.Collections.Concurrent;

namespace ShoppingList.Api.Services;

public class HubConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionState> _connections = new();

    private sealed class ConnectionState
    {
        public Guid UserId { get; init; }
        public HashSet<Guid> ListIds { get; } = [];
    }

    public void TrackJoin(string connectionId, Guid userId, Guid listId)
    {
        var state = _connections.GetOrAdd(connectionId, _ => new ConnectionState { UserId = userId });
        lock (state.ListIds)
        {
            state.ListIds.Add(listId);
        }
    }

    public void TrackLeave(string connectionId, Guid listId)
    {
        if (!_connections.TryGetValue(connectionId, out var state))
        {
            return;
        }

        lock (state.ListIds)
        {
            state.ListIds.Remove(listId);
        }
    }

    public IReadOnlyList<string> Disconnect(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out _))
        {
            return [];
        }

        return [connectionId];
    }

    public IReadOnlyList<Guid> GetListIds(string connectionId)
    {
        if (!_connections.TryGetValue(connectionId, out var state))
        {
            return [];
        }

        lock (state.ListIds)
        {
            return state.ListIds.ToList();
        }
    }

    public IReadOnlyList<string> GetConnectionIdsForUserInList(Guid userId, Guid listId)
    {
        return _connections
            .Where(pair =>
            {
                if (pair.Value.UserId != userId)
                {
                    return false;
                }

                lock (pair.Value.ListIds)
                {
                    return pair.Value.ListIds.Contains(listId);
                }
            })
            .Select(pair => pair.Key)
            .ToList();
    }

    public IReadOnlyList<string> GetConnectionIdsInList(Guid listId)
    {
        return _connections
            .Where(pair =>
            {
                lock (pair.Value.ListIds)
                {
                    return pair.Value.ListIds.Contains(listId);
                }
            })
            .Select(pair => pair.Key)
            .ToList();
    }
}
