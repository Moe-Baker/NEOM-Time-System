using System;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorRewindRecorderComponent : MonoBehaviour
{
    List<RewindRecorder> Recorders;

    public class StateRecorder : RewindSnapshotRecorder<StateSnapshot>
    {
        Animator Animator;

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
            return new StateSnapshot(Animator.speed);
        }
        protected override void ApplySnapshot(in StateSnapshot snapshot, SnapshotApplyConfiguration configuration)
        {
            switch (configuration.Source)
            {
                case SnapshotApplySource.Replication:
                    Animator.enabled = false;
                    break;

                case SnapshotApplySource.Simulate:
                    Animator.enabled = true;
                    break;

                default: throw new NotImplementedException();
            }
        }

        public StateRecorder(Animator Animator)
        {
            this.Animator = Animator;
        }
    }
    public struct StateSnapshot
    {
        public float Speed { get; }

        public StateSnapshot(float Speed)
        {
            this.Speed = Speed;
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
        Animator Animator;
        int LayerIndex;

        protected override LayerSnapshot CreateSnapshot()
        {
            var info = Animator.GetCurrentAnimatorStateInfo(LayerIndex);
            return new LayerSnapshot(info.shortNameHash, info.normalizedTime);
        }
        protected override void ApplySnapshot(in LayerSnapshot snapshot, SnapshotApplyConfiguration configuration)
        {
            if (configuration.Source == SnapshotApplySource.Simulate)
                Animator.Play(snapshot.StateHash, LayerIndex, snapshot.NormalizedTime);
        }

        public LayerRecorder(Animator Animator, int LayerIndex)
        {
            this.Animator = Animator;
            this.LayerIndex = LayerIndex;
        }
    }
    public struct LayerSnapshot
    {
        public int StateHash { get; }
        public float NormalizedTime { get; }

        public LayerSnapshot(int StateHash, float NormalizedTime)
        {
            this.StateHash = StateHash;
            this.NormalizedTime = NormalizedTime;
        }
    }

    void Awake()
    {
        var animator = GetComponent<Animator>();

        var skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        //Initialize Recorders List Capacity
        {
            var capacity = 0;

            capacity += 1; //State

            if (animator.applyRootMotion) capacity += 1;

            //Bones
            foreach (var skin in skins)
                capacity += skin.bones.Length;

            //Layers
            capacity += animator.layerCount;

            Recorders = new List<RewindRecorder>(capacity);
        }

        //Create State Recorder
        {
            var recorder = new StateRecorder(animator);
            Recorders.Add(recorder);
        }

        //Create Root Motion Recorder
        {
            var recorder = new TransformRewindRecorder(animator.transform);
            Recorders.Add(recorder);
        }

        //Create Bone Recorders
        {
            foreach (var skin in skins)
            {
                foreach (var bone in skin.bones)
                {
                    var recorder = new BoneRecorder(bone);
                    Recorders.Add(recorder);
                }
            }
        }

        //Create Layer Recorder
        {
            for (int i = 0; i < animator.layerCount; i++)
            {
                var recorder = new LayerRecorder(animator, i);
                Recorders.Add(recorder);
            }
        }

        //Ensure that animator is update so that no T-Pose is recorded
        animator.Update(Time.deltaTime);

        foreach (var recorder in Recorders)
            recorder.Begin();
    }
    void OnDestroy()
    {
        foreach (var recorder in Recorders)
            recorder.End();
    }
}