using UnityEditor;

using UnityEngine;

[RequireComponent(typeof(TimeEntity))]
public class BallProjectile : MonoBehaviour, IRewindStateSource<BallProjectile.State>
{
    [SerializeField]
    float DespawnDelay = 2f;

    bool MarkedForDespawn;
    float DespawnTimestamp;

    public State CreateSnapshot() => new State()
    {
        DespawnTimestamp = DespawnTimestamp,
        MarkedForDespawn = MarkedForDespawn,
    };
    public void ApplySnapshot(in State snapshot, SnapshotApplyConfiguration configuration)
    {
        DespawnTimestamp = snapshot.DespawnTimestamp;
        MarkedForDespawn = snapshot.MarkedForDespawn;
    }
    public bool CheckChange(in State a, in State b)
    {
        if (RewindRecorder.ChangeChecker.CheckChange(a.DespawnTimestamp, b.DespawnTimestamp)) return true;
        if (RewindRecorder.ChangeChecker.CheckChange(a.MarkedForDespawn, b.MarkedForDespawn)) return true;

        return false;
    }
    public struct State
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