using System.Collections.Generic;

using UnityEngine;

public abstract class RewindRecorderComponent<TRecorder> : MonoBehaviour
    where TRecorder : RewindRecorder, new()
{
    protected TRecorder Recorder;

    void Awake()
    {
        Recorder = new TRecorder();

        AssignTarget();

        Recorder.Begin();
    }
    void OnDestroy()
    {
        Recorder.End();
    }

    public abstract void AssignTarget();
}

public abstract class RewindRecorder
{
    protected static RewindSystem RewindSystem => RewindSystem.Instance;

    public TimeEntity Entity { get; private set; }
    public void Set(TimeEntity reference)
    {
        Entity = reference;
    }

    public virtual void Begin() { }
    public virtual void End() { }
}

public abstract class RewindSnapshotRecorder<TSnapshot> : RewindRecorder
{
    int Anchor;
    RingBuffer<TSnapshot> Snapshots;

    public override void Begin()
    {
        base.Begin();

        RewindSystem.OnCapture += Capture;
        RewindSystem.OnReplicate += Replicate;
        RewindSystem.OnSimulate += Simulate;
    }
    public override void End()
    {
        base.End();

        RewindSystem.OnCapture -= Capture;
        RewindSystem.OnReplicate -= Replicate;
        RewindSystem.OnSimulate -= Simulate;
    }

    protected virtual void Capture(RewindCaptureContext context)
    {
        var snapshot = CreateSnapshot();
        Snapshots.Push(snapshot);

        Anchor = context.Tick;
    }
    protected virtual void Replicate(RewindPlaybackContext context)
    {
        var depth = Anchor - context.Tick;
        var index = ^(1 + depth);

        if (Snapshots.HasIndex(index))
        {
            ref var snapshot = ref Snapshots[index];
            var configuration = new SnapshotApplyConfiguration(SnapshotApplySource.Replication);
            ApplySnapshot(in snapshot, configuration);
        }
    }
    protected virtual void Simulate(RewindSimulationContext context)
    {
        var diff = Anchor - context.Tick;
        Anchor = context.Tick;

        //Clear Discarded Snapshots
        for (int i = 0; i < diff; i++)
        {
            if (Snapshots.Count is 0)
                break;

            Snapshots.Pop();
        }

        //Simulate Last snapshot
        if (Snapshots.Count > 0)
        {
            ref var snapshot = ref Snapshots[^1];
            var configuration = new SnapshotApplyConfiguration(SnapshotApplySource.Simulate);

            ApplySnapshot(in snapshot, configuration);
        }
    }

    protected abstract TSnapshot CreateSnapshot();

    protected abstract void ApplySnapshot(in TSnapshot snapshot, SnapshotApplyConfiguration configuration);
    public struct SnapshotApplyConfiguration
    {
        public SnapshotApplySource Source { get; }

        public SnapshotApplyConfiguration(SnapshotApplySource Source)
        {
            this.Source = Source;
        }
    }
    public enum SnapshotApplySource
    {
        Replication, Simulate
    }

    protected RewindSnapshotRecorder()
    {
        Snapshots = new RingBuffer<TSnapshot>(RewindSystem.MaxSnapshotsCapacity);
    }
}