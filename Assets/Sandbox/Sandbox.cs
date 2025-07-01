using UnityEngine;
using UnityEngine.InputSystem;

public class Sandbox : MonoBehaviour, IRewindStateSource<Sandbox.Snapshot>
{
    int Clicks;

    RewindSystem RewindSystem => RewindSystem.Instance;

    StateRewindRecorder<Sandbox, Snapshot> StateRecorder;
    public Snapshot CreateSnapshot() => new Snapshot()
    {
        Clicks = Clicks,
    };
    public void ApplySnapshot(in Snapshot snapshot, SnapshotApplyConfiguration configuration)
    {
        Clicks = snapshot.Clicks;
    }
    public struct Snapshot
    {
        public int Clicks;
    }

    void Awake()
    {
        StateRecorder = new StateRewindRecorder<Sandbox, Snapshot>(this);
        StateRecorder.Begin();
    }
    void OnDestroy()
    {
        StateRecorder.End();
    }

    void Update()
    {
        if (RewindSystem.Timeline.State is TimelineState.Paused)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            Clicks += 1;
    }
}
