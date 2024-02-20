using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyNetDaemon.apps.Common;

public class LightStateStore
{
    private readonly ConcurrentDictionary<string, LightStateStoreEntry> _states = new();

    public LightStateStore(IEnumerable<string> topics)
    {
        _states = new ConcurrentDictionary<string, LightStateStoreEntry>(topics.ToDictionary(t => t,
            t => new LightStateStoreEntry()));
    }

    public async Task<LightState>? GetStateAsync(string zigbeeTopic, Func<string, Task> publishFunc)
    {
        //if (_states.TryGetValue(zigbeeTopic, out var entry) && entry.LastUpdated < DateTimeOffset.UtcNow.AddSeconds(-2))
        //{
        //    return entry.State;
        //}
        _states[zigbeeTopic] = new LightStateStoreEntry() {GetState = new TaskCompletionSource<LightState>()};
        await publishFunc(zigbeeTopic);
        return await _states[zigbeeTopic].GetStateAsync;
    }

    public Task ProcessMessageAsync(string topic, ArraySegment<byte> payload)
    {
        if (!_states.ContainsKey(topic))
        {
            return Task.CompletedTask;
        }
        Debug.WriteLine($"Processing '{topic}' with message {Encoding.UTF8.GetString(payload.Array)}");
        var lightState = JsonSerializer.Deserialize<LightState>(payload);
        if (_states.TryGetValue(topic, out var state))
        {
            state.SetState(lightState);

            return Task.CompletedTask;
        }

        _states[topic] = new LightStateStoreEntry() {LastUpdated = DateTimeOffset.UtcNow, State = lightState};

        return Task.CompletedTask;
    }

    private class LightStateStoreEntry
    {
        public LightState? State { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public void SetState(LightState state)
        {
            State = state;
            LastUpdated = DateTimeOffset.Now;
            if (GetState != null && !GetState.Task.IsCompleted)
            {
                GetState.SetResult(state);
            }
        }

        public Task<LightState> GetStateAsync => GetState?.Task;

        public TaskCompletionSource<LightState> GetState { get; set; } = new TaskCompletionSource<LightState>();
    }

}


