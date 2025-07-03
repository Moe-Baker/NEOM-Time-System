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

    [Serializable]
    public class ComponentRecorder : RewindSnapshotRecorder<ComponentState>
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
            Animator.applyRootMotion = false;
        }

        protected override ComponentState CreateState()
        {
            return new ComponentState(Animator.enabled, Animator.applyRootMotion);
        }
        protected override void ApplyState(in ComponentState snapshot, SnapshotApplyConfiguration configuration)
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
        protected override bool CheckChange(in ComponentState a, in ComponentState b)
        {
            if (ChangeChecker.CheckChange(a.Enabled, b.Enabled)) return true;
            if (ChangeChecker.CheckChange(a.ApplyRootMotion, b.ApplyRootMotion)) return true;

            return false;
        }

        public ComponentRecorder(AnimatorRewindRecorderComponent Component)
        {
            this.Component = Component;
        }
    }
    public struct ComponentState
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

        public ComponentState(bool Enabled, bool ApplyRootMotion)
        {
            Booleans = BooleanStates.None;

            if (Enabled) Booleans |= BooleanStates.Enabled;
            if (ApplyRootMotion) Booleans |= BooleanStates.ApplyRootMotion;
        }
    }

    [Serializable]
    public class BoneRecorder : RewindSnapshotRecorder<BoneState>
    {
        Transform Target;
        public void SetTarget(Transform value)
        {
            Target = value;
        }

        protected override BoneState CreateState() => new BoneState(Target.localRotation);
        protected override void ApplyState(in BoneState snapshot, SnapshotApplyConfiguration configuration)
        {
            Target.localRotation = snapshot.Rotation;
        }
        protected override bool CheckChange(in BoneState a, in BoneState b)
        {
            if (ChangeChecker.CheckChange(a.Rotation, b.Rotation)) return true;

            return false;
        }

        public BoneRecorder(Transform Target)
        {
            SetTarget(Target);
        }
    }
    public struct BoneState
    {
        public Quaternion Rotation { get; private set; }

        public BoneState(Quaternion Rotation)
        {
            this.Rotation = Rotation;
        }
    }

    [Serializable]
    public class LayerRecorder : RewindSnapshotRecorder<LayerState>
    {
        AnimatorRewindRecorderComponent Component;
        Animator Animator => Component.Animator;

        int LayerIndex;

        protected override LayerState CreateState()
        {
            var weight = Animator.GetLayerWeight(LayerIndex);
            var info = Animator.GetCurrentAnimatorStateInfo(LayerIndex);

            return new LayerState(weight, info.shortNameHash, info.normalizedTime);
        }
        protected override void ApplyState(in LayerState snapshot, SnapshotApplyConfiguration configuration)
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
        protected override bool CheckChange(in LayerState a, in LayerState b)
        {
            if (ChangeChecker.CheckChange(a.Weight, b.Weight)) return true;
            if (ChangeChecker.CheckChange(a.StateHash, b.StateHash)) return true;
            if (ChangeChecker.CheckChange(a.NormalizedTime, b.NormalizedTime)) return true;

            return false;
        }

        public LayerRecorder(AnimatorRewindRecorderComponent Component, int LayerIndex)
        {
            this.Component = Component;
            this.LayerIndex = LayerIndex;
        }
    }
    public struct LayerState
    {
        public float Weight { get; }
        public int StateHash { get; }
        public float NormalizedTime { get; }

        public LayerState(float Weight, int StateHash, float NormalizedTime)
        {
            this.Weight = Weight;
            this.StateHash = StateHash;
            this.NormalizedTime = NormalizedTime;
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

        //Create Component Recorder
        {
            var recorder = new ComponentRecorder(this);
            Recorders.Add(recorder);
        }

        //Create Root Motion Recorder
        if (Animator.applyRootMotion)
        {
            var recorder = new TransformRewindRecorder(Animator.transform);
            Recorders.Add(recorder);
        }

        //Create Layer Recorder
        for (int i = 0; i < Animator.layerCount; i++)
        {
            var recorder = new LayerRecorder(this, i);
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