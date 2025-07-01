using UnityEngine;

public class TransformRewindRecorderComponent : RewindRecorderComponent<TransformRewindRecorder>
{
    public override void AssignTarget()
    {
        Recorder.SetTarget(transform);
    }
}

public class TransformRewindRecorder : RewindSnapshotRecorder<TransformRewindSnapshot>
{
    public Transform Target { get; private set; }
    public void SetTarget(Transform value)
    {
        Target = value;
    }

    protected override TransformRewindSnapshot CreateSnapshot()
    {
        return new TransformRewindSnapshot(Target.position, Target.rotation, Target.localScale);
    }
    protected override void ApplySnapshot(in TransformRewindSnapshot snapshot, SnapshotApplyConfiguration configuration)
    {
        Target.position = snapshot.Position;
        Target.rotation = snapshot.Rotation;
        Target.localScale = snapshot.Scale;
    }

    public TransformRewindRecorder() { }
    public TransformRewindRecorder(Transform Target)
    {
        SetTarget(Target);
    }
}

public struct TransformRewindSnapshot
{
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }

    public TransformRewindSnapshot(Vector3 Position, Quaternion Rotation, Vector3 Scale)
    {
        this.Position = Position;
        this.Rotation = Rotation;
        this.Scale = Scale;
    }
}