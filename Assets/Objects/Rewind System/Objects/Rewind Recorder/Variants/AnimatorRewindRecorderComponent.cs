using System;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorRewindRecorderComponent : MonoBehaviour
{
    Animator Animator;

    List<RewindRecorder> Recorders;

    [SerializeField]
    SourceFlags Source = SourceFlags.Bones | SourceFlags.State;
    [Flags]
    public enum SourceFlags
    {
        None = 0,

        Bones = 1 << 0,
        State = 1 << 1,

        Everything = ~0,
    }

    public class StateRecorder : RewindSnapshotRecorder<StateSnapshot>
    {
        AnimatorRewindRecorderComponent Component;
        Animator Animator => Component.Animator;

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
            Animator.enabled = false;
        }

        protected override StateSnapshot CreateSnapshot()
        {
            return new StateSnapshot(Animator.enabled, Animator.applyRootMotion);
        }
        protected override void ApplySnapshot(in StateSnapshot snapshot, SnapshotApplyConfiguration configuration)
        {
            switch (configuration.Source)
            {
                case SnapshotApplySource.Replication:
                    Animator.enabled = false;
                    Animator.applyRootMotion = false;
                    break;

                case SnapshotApplySource.Simulate:
                    Animator.enabled = snapshot.Enabled;
                    Animator.applyRootMotion = snapshot.ApplyRootMotion;
                    break;

                default: throw new NotImplementedException();
            }
        }

        public StateRecorder(AnimatorRewindRecorderComponent Component)
        {
            this.Component = Component;
        }
    }
    public struct StateSnapshot
    {
        BooleanStates Booleans;
        [Flags]
        enum BooleanStates : byte
        {
            None = 0,

            Enabled = 1 << 0,
            ApplyRootMotion = 1 << 1
        }

        public bool Enabled => Booleans.HasFlag(BooleanStates.Enabled);
        public bool ApplyRootMotion => Booleans.HasFlag(BooleanStates.ApplyRootMotion);

        public StateSnapshot(bool Enabled, bool ApplyRootMotion)
        {
            Booleans = BooleanStates.None;

            if (Enabled) Booleans |= BooleanStates.Enabled;
            if (ApplyRootMotion) Booleans |= BooleanStates.ApplyRootMotion;
        }
    }

    public class BoneRecorder : RewindSnapshotRecorder<BoneSnapshot>
    {
        Transform Target;
        public void SetTarget(Transform value)
        {
            Target = value;
        }

        protected override BoneSnapshot CreateSnapshot() => new BoneSnapshot(Target.localRotation);
        protected override void ApplySnapshot(in BoneSnapshot snapshot, SnapshotApplyConfiguration configuration)
        {
            Target.localRotation = snapshot.Rotation;
        }

        public BoneRecorder(Transform Target)
        {
            SetTarget(Target);
        }
    }
    public struct BoneSnapshot
    {
        public Quaternion Rotation { get; private set; }

        public BoneSnapshot(Quaternion Rotation)
        {
            this.Rotation = Rotation;
        }
    }

    public class LayerRecorder : RewindSnapshotRecorder<LayerSnapshot>
    {
        AnimatorRewindRecorderComponent Component;
        Animator Animator => Component.Animator;

        int LayerIndex;

        protected override LayerSnapshot CreateSnapshot()
        {
            var info = Animator.GetCurrentAnimatorStateInfo(LayerIndex);
            var weight = Animator.GetLayerWeight(LayerIndex);

            return new LayerSnapshot(info.shortNameHash, info.normalizedTime, weight);
        }
        protected override void ApplySnapshot(in LayerSnapshot snapshot, SnapshotApplyConfiguration configuration)
        {
            switch (configuration.Source)
            {
                case SnapshotApplySource.Replication:
                {
                    if (Component.Source.HasFlag(SourceFlags.State))
                    {
                        Animator.SetLayerWeight(LayerIndex, snapshot.Weight);
                        Animator.Play(snapshot.StateHash, LayerIndex, snapshot.NormalizedTime);

                        Animator.Update(0);
                    }
                }
                break;

                case SnapshotApplySource.Simulate:
                {
                    Animator.SetLayerWeight(LayerIndex, snapshot.Weight);
                    Animator.Play(snapshot.StateHash, LayerIndex, snapshot.NormalizedTime);

                    Animator.Update(0);
                }
                break;

                default: throw new NotImplementedException();
            }
        }

        public LayerRecorder(AnimatorRewindRecorderComponent Component, int LayerIndex)
        {
            this.Component = Component;
            this.LayerIndex = LayerIndex;
        }
    }
    public struct LayerSnapshot
    {
        public int StateHash { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }

        public LayerSnapshot(int StateHash, float NormalizedTime, float Weight)
        {
            this.StateHash = StateHash;
            this.NormalizedTime = NormalizedTime;
            this.Weight = Weight;
        }
    }

    void Awake()
    {
        Animator = GetComponent<Animator>();

        var skins = Array.Empty<SkinnedMeshRenderer>();

        //Initialize Recorders List Capacity
        {
            var capacity = 0;

            capacity += 1; //State

            if (Animator.applyRootMotion) capacity += 1;

            //Bones
            if (Source.HasFlag(SourceFlags.Bones))
            {
                skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);

                foreach (var skin in skins)
                    capacity += skin.bones.Length;
            }

            //Layers
            capacity += Animator.layerCount;

            Recorders = new List<RewindRecorder>(capacity);
        }

        //Create State Recorder
        {
            var recorder = new StateRecorder(this);
            Recorders.Add(recorder);
        }

        //Create Root Motion Recorder
        if (Animator.applyRootMotion)
        {
            var recorder = new TransformRewindRecorder(Animator.transform);
            Recorders.Add(recorder);
        }

        //Create Bone Recorders
        foreach (var skin in skins)
        {
            foreach (var bone in skin.bones)
            {
                var recorder = new BoneRecorder(bone);
                Recorders.Add(recorder);
            }
        }

        //Create Layer Recorder
        for (int i = 0; i < Animator.layerCount; i++)
        {
            var recorder = new LayerRecorder(this, i);
            Recorders.Add(recorder);
        }

        //Ensure that animator is update so that no T-Pose is recorded
        Animator.Update(Time.deltaTime);

        foreach (var recorder in Recorders)
            recorder.Begin();
    }
    void OnDestroy()
    {
        foreach (var recorder in Recorders)
            recorder.End();
    }
}