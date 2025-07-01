using System;

using UnityEngine;

public class TimeEntity : MonoBehaviour
{
    LifetimeRecorder Lifetime;
    public class LifetimeRecorder : RewindSnapshotRecorder<LifetimeSnapshot>
    {
        RewindTick SpawnTick;
        RewindTick DespawnTick;

        public override void Begin()
        {
            base.Begin();

            SpawnTick = RewindSystem.Timeline.AnchorTick;
            DespawnTick = RewindTick.Max;

            Entity.OnDespawn += EntityDespawnCallback;
            Entity.OnRespawn += EntityRespawnCallback;

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
                Entity.Destroy();
            }
        }

        protected override void Replicate(RewindPlaybackContext context)
        {
            var state = CheckSpawnState(context.Tick);

            switch (state)
            {
                case SpawnState.UnSpawned:
                case SpawnState.Despawned:
                    Entity.SetActive(false);
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
                    Entity.Destroy();
                    break;

                case SpawnState.Despawned:
                    Entity.SetActive(false);
                    break;

                case SpawnState.Live:
                {
                    if (Entity.IsSpawned is false)
                        Entity.Respawn();

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

        protected override LifetimeSnapshot CreateSnapshot()
        {
            return new LifetimeSnapshot(Entity.gameObject.activeSelf);
        }
        protected override void ApplySnapshot(in LifetimeSnapshot snapshot, SnapshotApplyConfiguration configuration)
        {
            Entity.SetActive(snapshot.IsActive);
        }
    }
    public struct LifetimeSnapshot
    {
        public bool IsActive { get; }
        public LifetimeSnapshot(bool IsActive)
        {
            this.IsActive = IsActive;
        }
    }

    void Awake()
    {
        IsSpawned = true;

        Lifetime = new LifetimeRecorder();
        Lifetime.Set(this);
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