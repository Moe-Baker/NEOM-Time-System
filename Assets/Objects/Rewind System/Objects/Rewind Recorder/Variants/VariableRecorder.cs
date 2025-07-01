using System;

using UnityEngine;

[Serializable]
public class VariableRecorder<T> : RewindSnapshotRecorder<T>
    where T : struct
{
    public T Value { get; set; }

    protected override T CreateSnapshot() => Value;

    protected override void ApplySnapshot(in T snapshot, SnapshotApplyConfiguration configuration)
    {
        Value = snapshot;

        OnApplySnapshot?.Invoke(configuration);
    }
    public delegate void ApplySnapshotDelegate(SnapshotApplyConfiguration configuration);
    public event ApplySnapshotDelegate OnApplySnapshot;
}