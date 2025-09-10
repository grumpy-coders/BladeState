using System;
using BladeState.Enums;

namespace BladeState.Events
{
    public class BladeStateProviderEventArgs<T>(string instanceId, T state, ProviderEventType eventType) : EventArgs where T : class, new()
    {
        public string InstanceId { get; set; } = instanceId;
        public T State { get; set; } = state;
        public ProviderEventType EventType { get; set; } = eventType;
    }
}
