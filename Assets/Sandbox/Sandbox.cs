using UnityEngine;
using UnityEngine.InputSystem;

public class Sandbox : MonoBehaviour, IRewindStateSource<Sandbox.State>
{
    int Clicks;

    RewindSystem RewindSystem => RewindSystem.Instance;

    StateRewindRecorder<Sandbox, State> StateRecorder;
    public State CreateSnapshot() => new State()
    {
        Clicks = Clicks,
    };
    public void ApplySnapshot(in State snapshot, SnapshotApplyConfiguration configuration)
    {
        Clicks = snapshot.Clicks;
    }
    public bool CheckChange(in State a, in State b)
    {
        if (RewindRecorder.ChangeChecker.CheckChange(a.Clicks, b.Clicks)) return true;

        return false;
    }

    public struct State
    {
        public int Clicks;
    }

    void Awake()
    {
        StateRecorder = new StateRewindRecorder<Sandbox, State>(this);
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
