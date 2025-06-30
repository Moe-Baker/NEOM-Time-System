using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyRewindRecorderComponent : RewindRecorderComponent<RigidbodyRewindRecorder>
{
    public override void AssignTarget()
    {
        var rigidbody = GetComponent<Rigidbody>();
        Recorder.SetTarget(rigidbody);
    }
}

public class RigidbodyRewindRecorder : RewindSnapshotRecorder<RigidbodyRewindSnapshot>
{
    public Rigidbody Target { get; private set; }
    public void SetTarget(Rigidbody value)
    {
        Target = value;
    }

    public override void Begin()
    {
        base.Begin();

        RewindSystem.Timeline.OnPause += TimelinePauseCallback;
    }
    public override void End()
    {
        base.End();

        RewindSystem.Timeline.OnPause -= TimelinePauseCallback;
    }

    void TimelinePauseCallback()
    {
        Target.isKinematic = true;
    }

    protected override RigidbodyRewindSnapshot CreateSnapshot()
    {
        var position = (Target.interpolation is RigidbodyInterpolation.None) ? Target.position : Target.transform.position;
        var rotation = (Target.interpolation is RigidbodyInterpolation.None) ? Target.rotation : Target.transform.rotation;

        return new RigidbodyRewindSnapshot(Target.isKinematic, position, Target.linearVelocity, rotation, Target.angularVelocity);
    }
    protected override void ApplySnapshot(in RigidbodyRewindSnapshot snapshot, SnapshotApplyConfiguration configuration)
    {
        Target.position = snapshot.Position;
        Target.rotation = snapshot.Rotation;

        switch (configuration.Source)
        {
            case SnapshotApplySource.Replication:
            {
                Target.isKinematic = true;
            }
            break;

            case SnapshotApplySource.Simulate:
            {
                Target.isKinematic = snapshot.IsKinematic;
                Target.linearVelocity = snapshot.LinearVelocity;
                Target.angularVelocity = snapshot.AngularVelocity;
            }
            break;
        }
    }
}

public struct RigidbodyRewindSnapshot
{
    public bool IsKinematic { get; }

    public Vector3 Position { get; }
    public Vector3 LinearVelocity { get; }

    public Quaternion Rotation { get; }
    public Vector3 AngularVelocity { get; }

    public RigidbodyRewindSnapshot(bool IsKinematic, Vector3 Position, Vector3 LinearVelocity, Quaternion Rotation, Vector3 AngularVelocity)
    {
        this.IsKinematic = IsKinematic;
        this.Position = Position;
        this.LinearVelocity = LinearVelocity;

        this.Rotation = Rotation;
        this.AngularVelocity = AngularVelocity;
    }
}