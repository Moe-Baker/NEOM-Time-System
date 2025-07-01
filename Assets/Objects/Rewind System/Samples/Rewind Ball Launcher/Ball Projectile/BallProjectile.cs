using UnityEngine;

[RequireComponent(typeof(TimeEntity))]
public class BallProjectile : MonoBehaviour
{
    public Rigidbody Rigidbody { get; private set; }
    public TimeEntity TimeEntity { get; private set; }

    float DespawnTimestamp;

    RewindSystem RewindSystem => RewindSystem.Instance;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        TimeEntity = GetComponent<TimeEntity>();
    }

    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        if (RewindSystem.Timeline.State is TimelineState.Paused)
            return;

        if (TimeEntity.IsSpawned is false)
            return;

        TimeEntity.Despawn();
    }
}