using UnityEngine;

public class StateRewindRecorder<TSource, TState> : RewindSnapshotRecorder<TState>
    where TSource : IRewindStateSource<TState>
{
    TSource Source;
    public void SetSource(TSource value)
    {
        Source = value;
    }

    protected override TState CreateSnapshot() => Source.CreateSnapshot();
    protected override void ApplySnapshot(in TState snapshot, SnapshotApplyConfiguration configuration) => Source.ApplySnapshot(in snapshot, configuration);

    public StateRewindRecorder(TSource Source)
    {
        SetSource(Source);
    }
}

public interface IRewindStateSource<TState>
{
    TState CreateSnapshot();
    void ApplySnapshot(in TState snapshot, SnapshotApplyConfiguration configuration);
}