using System;
using System.Collections.Generic;

using UnityEngine;

[Serializable]
public class VariableRecorder<T> : RewindSnapshotRecorder<T>
    where T : struct
{
    T InternalValue;
    public T Value
    {
        get => InternalValue;
        set
        {
            InternalValue = value;
            IsDirty = true;
        }
    }

    bool IsDirty;

    IEqualityComparer<T> Comparer;

    protected override T CreateState() => Value;
    protected override void ApplyState(in T snapshot, SnapshotApplyConfiguration configuration)
    {
        Value = snapshot;

        OnApplySnapshot?.Invoke(configuration);
    }
    protected override bool CheckChange(in T a, in T b)
    {
        if (IsDirty is false)
            return false;

        IsDirty = false;

        if (Comparer.Equals(a, b))
            return false;

        return true;
    }

    public delegate void ApplySnapshotDelegate(SnapshotApplyConfiguration configuration);
    public event ApplySnapshotDelegate OnApplySnapshot;

    public VariableRecorder(T Value) : this(Value, EqualityComparer<T>.Default) { }
    public VariableRecorder(T Value, IEqualityComparer<T> Comparer)
    {
        this.Value = Value;
        this.Comparer = Comparer;
    }
}