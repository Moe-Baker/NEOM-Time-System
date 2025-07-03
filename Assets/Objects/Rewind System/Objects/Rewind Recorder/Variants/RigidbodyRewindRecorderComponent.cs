using System;

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

[Serializable]
public class RigidbodyRewindRecorder : RewindSnapshotRecorder<RigidbodyRewindState>
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

    protected override RigidbodyRewindState CreateState()
    {
        var position = (Target.interpolation is RigidbodyInterpolation.None) ? Target.position : Target.transform.position;
        var rotation = (Target.interpolation is RigidbodyInterpolation.None) ? Target.rotation : Target.transform.rotation;

        return new RigidbodyRewindState(Target.isKinematic, position, Target.linearVelocity, rotation, Target.angularVelocity);
    }
    protected override void ApplyState(in RigidbodyRewindState snapshot, SnapshotApplyConfiguration configuration)
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

    protected override bool CheckChange(in RigidbodyRewindState a, in RigidbodyRewindState b)
    {
        if (ChangeChecker.CheckChange(a.IsKinematic, b.IsKinematic)) return true;

        if (ChangeChecker.CheckChange(a.Position, b.Position)) return true;
        if (ChangeChecker.CheckChange(a.LinearVelocity, b.LinearVelocity)) return true;

        if (ChangeChecker.CheckChange(a.Rotation, b.Rotation)) return true;
        if (ChangeChecker.CheckChange(a.AngularVelocity, b.AngularVelocity)) return true;

        return false;
    }
}

public struct RigidbodyRewindState
{
    public bool IsKinematic { get; }

    public Vector3 Position { get; }
    public Vector3 LinearVelocity { get; }

    public Quaternion Rotation { get; }
    public Vector3 AngularVelocity { get; }

    public RigidbodyRewindState(bool IsKinematic, Vector3 Position, Vector3 LinearVelocity, Quaternion Rotation, Vector3 AngularVelocity)
    {
        this.IsKinematic = IsKinematic;
        this.Position = Position;
        this.LinearVelocity = LinearVelocity;

        this.Rotation = Rotation;
        this.AngularVelocity = AngularVelocity;
    }
}