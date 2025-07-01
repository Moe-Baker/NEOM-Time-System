using UnityEditor;

using UnityEngine;

[RequireComponent(typeof(TimeEntity))]
public class BallProjectile : MonoBehaviour, IRewindStateSource<BallProjectile.Snapshot>
{
    [SerializeField]
    float DespawnDelay = 2f;

    bool MarkedForDespawn;
    float DespawnTimestamp;

    public Snapshot CreateSnapshot() => new Snapshot()
    {
        DespawnTimestamp = DespawnTimestamp,
        MarkedForDespawn = MarkedForDespawn,
    };
    public void ApplySnapshot(in Snapshot snapshot, SnapshotApplyConfiguration configuration)
    {
        DespawnTimestamp = snapshot.DespawnTimestamp;
        MarkedForDespawn = snapshot.MarkedForDespawn;
    }
    public struct Snapshot
    {
        public float DespawnTimestamp;
        public bool MarkedForDespawn;
    }

    public Rigidbody Rigidbody { get; private set; }
    public TimeEntity TimeEntity { get; private set; }

    RewindSystem RewindSystem => RewindSystem.Instance;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        TimeEntity = GetComponent<TimeEntity>();

        MarkedForDespawn = false;
    }

    void Update()
    {
        if (RewindSystem.Timeline.State is TimelineState.Paused)
            return;

        if (TimeEntity.IsSpawned is false)
            return;

        if (MarkedForDespawn is false)
            return;

        if (RewindSystem.Timeline.AnchorTick.Timestamp >= DespawnTimestamp)
        {
            TimeEntity.Despawn();
            return;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (RewindSystem.Timeline.State is TimelineState.Paused)
            return;

        if (TimeEntity.IsSpawned is false)
            return;

        if (MarkedForDespawn) return;

        DespawnTimestamp = RewindSystem.Timeline.MaxTime + DespawnDelay;
        MarkedForDespawn = true;
    }
}