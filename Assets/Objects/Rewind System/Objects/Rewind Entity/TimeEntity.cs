using System;

using UnityEngine;

public class TimeEntity : MonoBehaviour
{
    LifetimeRecorder Lifetime;
    [Serializable]
    public class LifetimeRecorder : RewindSnapshotRecorder<LifetimeState>
    {
        RewindTick SpawnTick;
        RewindTick DespawnTick;

        public TimeEntity Target { get; private set; }
        public void SetTarget(TimeEntity reference)
        {
            Target = reference;
        }

        public override void Begin()
        {
            base.Begin();

            SpawnTick = RewindSystem.Timeline.AnchorTick;
            DespawnTick = RewindTick.Max;

            Target.OnDespawn += EntityDespawnCallback;
            Target.OnRespawn += EntityRespawnCallback;

            RewindSystem.OnDiscard += Discard;
        }
        public override void End()
        {
            base.End();

            RewindSystem.OnDiscard -= Discard;
        }

        void EntityDespawnCallback()
        {
            DespawnTick = RewindSystem.Timeline.AnchorTick;
        }
        void EntityRespawnCallback()
        {
            DespawnTick = RewindTick.Max;
        }

        void Discard(RewindDiscardContext context)
        {
            if (context.Tick.Index >= DespawnTick.Index)
            {
                RewindSystem.OnDiscard -= Discard;
                Target.Destroy();
            }
        }

        protected override void Replicate(RewindPlaybackContext context)
        {
            var state = CheckSpawnState(context.Tick);

            switch (state)
            {
                case SpawnState.UnSpawned:
                case SpawnState.Despawned:
                    Target.SetActive(false);
                    break;

                case SpawnState.Live:
                    base.Replicate(context);
                    break;
            }
        }
        protected override void Simulate(RewindSimulationContext context)
        {
            var state = CheckSpawnState(context.Tick);

            switch (state)
            {
                case SpawnState.UnSpawned:
                    Target.Destroy();
                    break;

                case SpawnState.Despawned:
                    Target.SetActive(false);
                    break;

                case SpawnState.Live:
                {
                    if (Target.IsSpawned is false)
                        Target.Respawn();

                    base.Simulate(context);
                }
                break;
            }
        }

        SpawnState CheckSpawnState(RewindTick tick)
        {
            if (tick.Index < SpawnTick.Index)
                return SpawnState.UnSpawned;

            if (tick.Index > DespawnTick.Index)
                return SpawnState.Despawned;

            return SpawnState.Live;
        }
        public enum SpawnState
        {
            UnSpawned, Live, Despawned
        }

        protected override LifetimeState CreateState()
        {
            return new LifetimeState(Target.gameObject.activeSelf);
        }
        protected override void ApplyState(in LifetimeState snapshot, SnapshotApplyConfiguration configuration)
        {
            Target.SetActive(snapshot.IsActive);
        }
        protected override bool CheckChange(in LifetimeState a, in LifetimeState b)
        {
            if (ChangeChecker.CheckChange(a.IsActive, b.IsActive)) return true;

            return false;
        }

        public LifetimeRecorder(TimeEntity Target)
        {
            SetTarget(Target);
        }
    }
    public struct LifetimeState
    {
        public bool IsActive { get; }
        public LifetimeState(bool IsActive)
        {
            this.IsActive = IsActive;
        }
    }

    void Awake()
    {
        IsSpawned = true;

        Lifetime = new LifetimeRecorder(this);
        Lifetime.Begin();
    }
    void OnDestroy()
    {
        Lifetime.End();
    }

    public bool IsSpawned { get; private set; }

    void SetActive(bool value) => gameObject.SetActive(value);

    public void Despawn()
    {
        IsSpawned = false;

        SetActive(IsSpawned);

        OnDespawn?.Invoke();
    }
    public event Action OnDespawn;

    void Respawn()
    {
        IsSpawned = true;

        OnRespawn?.Invoke();
    }
    public event Action OnRespawn;

    void Destroy() => Destroy(gameObject);
}