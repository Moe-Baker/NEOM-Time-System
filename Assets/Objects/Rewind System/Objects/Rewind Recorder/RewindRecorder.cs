using System;
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

[Serializable]
public abstract class RewindRecorder
{
    protected static RewindSystem RewindSystem => RewindSystem.Instance;

    public virtual void Begin() { }
    public virtual void End() { }

    /// <summary>
    /// Quick utility to check if a value was changed, will return true if changed
    /// </summary>
    public static class ChangeChecker
    {
        public const float DefaultEpsilon = 0.00001f;

        public static bool CheckChange(bool a, bool b) => a != b;

        public static bool CheckChange(int a, int b) => CheckChange(a, b, 0);
        public static bool CheckChange(int a, int b, int epsilon)
        {
            return Mathf.Abs(a - b) > epsilon;
        }

        public static bool CheckChange(float a, float b) => CheckChange(a, b, DefaultEpsilon);
        public static bool CheckChange(float a, float b, float epsilon)
        {
            return Mathf.Abs(a - b) > epsilon;
        }

        public static bool CheckChange(Vector2 a, Vector2 b) => CheckChange(a, b, DefaultEpsilon);
        public static bool CheckChange(Vector2 a, Vector2 b, float epsilon)
        {
            return Vector2.SqrMagnitude(a - b) > epsilon;
        }

        public static bool CheckChange(Vector3 a, Vector3 b) => CheckChange(a, b, DefaultEpsilon);
        public static bool CheckChange(Vector3 a, Vector3 b, float epsilon)
        {
            var diff = (a - b);
            var mgt = Vector3.SqrMagnitude(diff);

            return mgt > epsilon;
        }

        public static bool CheckChange(Quaternion a, Quaternion b) => CheckChange(a, b, DefaultEpsilon);
        public static bool CheckChange(Quaternion a, Quaternion b, float epsilon)
        {
            return Quaternion.Angle(a, b) > epsilon;
        }
    }
}

[Serializable]
public abstract class RewindSnapshotRecorder<TState> : RewindRecorder
{
    RewindTick AnchorTick;
    RingBuffer<Snapshot> Snapshots;
    public struct Snapshot
    {
        public readonly int Tick;
        public readonly TState State;

        public Snapshot(int Tick, TState State)
        {
            this.Tick = Tick;
            this.State = State;
        }
    }

    bool TryGetSnapshot(int tick, out Snapshot snapshot)
    {
        if (TryIndexSnapshot(tick, out int index) is false)
        {
            snapshot = default;
            return false;
        }

        snapshot = Snapshots[index];
        return true;
    }
    bool TryIndexSnapshot(int tick, out int index)
    {
        if (Snapshots.Count is 0)
        {
            index = default;
            return false;
        }

        if (tick < Snapshots[0].Tick || tick > AnchorTick.Index)
        {
            index = default;
            return false;
        }

        var low = 0;
        var high = Snapshots.Count - 1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            ref var element = ref Snapshots[mid];

            if (element.Tick == tick)
            {
                index = mid;
                return true;
            }

            if (tick > element.Tick)
                low = mid + 1;
            else
                high = mid - 1;
        }

        (low, high) = (high, low);

        index = low;
        return true;
    }

    public override void Begin()
    {
        base.Begin();

        Snapshots = new RingBuffer<Snapshot>(RewindSystem.MaxSnapshotsCapacity);

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
        AnchorTick = context.Tick;

        var state = CreateState();

        if (Snapshots.Count > 0 && CheckChange(in state, in Snapshots[^1].State) is false)
            return;

        var snapshot = new Snapshot(context.Tick.Index, state);
        Snapshots.Push(snapshot);
    }
    /// <summary>
    /// Measures if the two states have changed
    /// </summary>
    /// <returns>true if changed</returns>
    protected abstract bool CheckChange(in TState a, in TState b);
    protected abstract TState CreateState();

    protected virtual void Replicate(RewindPlaybackContext context)
    {
        if (TryGetSnapshot(context.Tick.Index, out var snapshot))
        {
            var configuration = new SnapshotApplyConfiguration(context.Tick, SnapshotApplySource.Replication);
            ApplyState(in snapshot.State, configuration);
        }
    }
    protected virtual void Simulate(RewindSimulationContext context)
    {
        AnchorTick = context.Tick;

        while (Snapshots.Count > 0)
        {
            ref var entry = ref Snapshots[^1];

            if (entry.Tick > AnchorTick.Index)
                Snapshots.Pop();
            else
                break;
        }

        if (TryGetSnapshot(context.Tick.Index, out var snapshot))
        {
            var configuration = new SnapshotApplyConfiguration(context.Tick, SnapshotApplySource.Simulate);
            ApplyState(snapshot.State, configuration);
        }
    }
    protected abstract void ApplyState(in TState snapshot, SnapshotApplyConfiguration configuration);
}

public struct SnapshotApplyConfiguration
{
    public RewindTick Tick { get; }
    public SnapshotApplySource Source { get; }

    public SnapshotApplyConfiguration(RewindTick Tick, SnapshotApplySource Source)
    {
        this.Tick = Tick;
        this.Source = Source;
    }
}
public enum SnapshotApplySource
{
    Replication, Simulate
}