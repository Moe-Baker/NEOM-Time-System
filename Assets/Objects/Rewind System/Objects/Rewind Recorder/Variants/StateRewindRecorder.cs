using System;

using UnityEngine;

[Serializable]
public class StateRewindRecorder<TSource, TState> : RewindSnapshotRecorder<TState>
    where TSource : IRewindStateSource<TState>
{
    TSource Source;
    public void SetSource(TSource value)
    {
        Source = value;
    }

    protected override TState CreateState() => Source.CreateSnapshot();
    protected override void ApplyState(in TState snapshot, SnapshotApplyConfiguration configuration) => Source.ApplySnapshot(in snapshot, configuration);
    protected override bool CheckChange(in TState a, in TState b) => Source.CheckChange(in a, in b);

    public StateRewindRecorder(TSource Source)
    {
        SetSource(Source);
    }
}

public interface IRewindStateSource<TState>
{
    TState CreateSnapshot();
    void ApplySnapshot(in TState snapshot, SnapshotApplyConfiguration configuration);

    /// <summary>
    /// <inheritdoc cref="RewindSnapshotRecorder{TState}.CheckChange(in TState, in TState)"/>
    /// </summary>
    /// <returns>
    /// <inheritdoc cref="RewindSnapshotRecorder{TState}.CheckChange(in TState, in TState)"/>
    /// </returns>
    bool CheckChange(in TState a, in TState b);
}