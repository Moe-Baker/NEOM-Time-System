using System;

using UnityEngine;

public class TransformRewindRecorderComponent : RewindRecorderComponent<TransformRewindRecorder>
{
    public override void AssignTarget()
    {
        Recorder.SetTarget(transform);
    }
}

[Serializable]
public class TransformRewindRecorder : RewindSnapshotRecorder<TransformRewindState>
{
    public Transform Target { get; private set; }
    public void SetTarget(Transform value)
    {
        Target = value;
    }

    protected override TransformRewindState CreateState()
    {
        return new TransformRewindState(Target.position, Target.rotation, Target.localScale);
    }
    protected override void ApplyState(in TransformRewindState snapshot, SnapshotApplyConfiguration configuration)
    {
        Target.position = snapshot.Position;
        Target.rotation = snapshot.Rotation;
        Target.localScale = snapshot.Scale;
    }
    protected override bool CheckChange(in TransformRewindState a, in TransformRewindState b)
    {
        if (ChangeChecker.CheckChange(a.Position, b.Position)) return true;
        if (ChangeChecker.CheckChange(a.Rotation, b.Rotation)) return true;
        if (ChangeChecker.CheckChange(a.Scale, b.Scale)) return true;

        return false;
    }

    public TransformRewindRecorder() { }
    public TransformRewindRecorder(Transform Target)
    {
        SetTarget(Target);
    }
}

public struct TransformRewindState
{
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }

    public TransformRewindState(Vector3 Position, Quaternion Rotation, Vector3 Scale)
    {
        this.Position = Position;
        this.Rotation = Rotation;
        this.Scale = Scale;
    }
}