using System;
using System.Collections.Generic;

using UnityEngine;

public class RewindSystem : MonoBehaviour
{
    [SerializeField, Tooltip("Max Desired FPS")]
    int MaxFPS = 60;

    [SerializeField, Tooltip("Max Recording Duration in Seconds")]
    float MaxDuration = 30;

    public int MaxSnapshotsCapacity => Mathf.CeilToInt(MaxFPS * MaxDuration);

    void Capture(RewindTick tick)
    {
        var context = new RewindCaptureContext(tick);
        OnCapture?.Invoke(context);
    }
    public event CaptureContextDelegate OnCapture;
    public delegate void CaptureContextDelegate(RewindCaptureContext context);

    void Discard(RewindTick tick)
    {
        var context = new RewindDiscardContext(tick);
        OnDiscard?.Invoke(context);
    }
    public event DiscardContextDelegate OnDiscard;
    public delegate void DiscardContextDelegate(RewindDiscardContext context);

    void Replicate(RewindTick tick)
    {
        var context = new RewindPlaybackContext(tick);
        OnReplicate?.Invoke(context);
    }
    public event PlaybackContextDelegate OnReplicate;
    public delegate void PlaybackContextDelegate(RewindPlaybackContext context);

    void Simulate(RewindTick tick)
    {
        var context = new RewindSimulationContext(tick);
        OnSimulate?.Invoke(context);
    }
    public delegate void SimulateDelegate(RewindSimulationContext context);
    public event SimulateDelegate OnSimulate;

    [field: SerializeField]
    public TimelineModule Timeline { get; private set; }
    [Serializable]
    public class TimelineModule : IModule<RewindSystem>
    {
        public RewindTick AnchorTick { get; private set; }

        RingBuffer<RewindTick> TickHistory;
        public int TickCount => TickHistory.Count;

        public float MaxTime => AnchorTick.Timestamp;
        public float MinTime => Mathf.Max(TickHistory[0].Timestamp, MaxTime - Rewind.MaxDuration);

        public TimelineState State { get; private set; }

        public float Scale
        {
            get
            {
                return State switch
                {
                    TimelineState.Live => 1f,
                    TimelineState.Paused => 0f,

                    _ => throw new NotImplementedException(),
                };
            }
        }

        RewindSystem Rewind;
        public void SetReference(RewindSystem reference)
        {
            Rewind = reference;

            State = TimelineState.Live;

            AnchorTick = new RewindTick(-1, 0f, 0f);
            TickHistory = new RingBuffer<RewindTick>(Rewind.MaxSnapshotsCapacity);

            Rewind.OnUpdate += Update;
        }

        public void Pause()
        {
            if (TickCount < 2)
                throw new InvalidOperationException($"Can Only Pause Rewind Timeline if More than 2 Ticks Are Recorded");

            State = TimelineState.Paused;

            OnPause?.Invoke();
        }
        public event Action OnPause;

        public void Resume()
        {
            State = TimelineState.Live;

            OnResume?.Invoke();

            Simulate();
        }
        public event Action OnResume;

        void Simulate()
        {
            Rewind.Simulate(AnchorTick);

            var diff = TickHistory[^1].Index - AnchorTick.Index;

            //Clear Discarded Snapshots
            for (int i = 0; i < diff; i++)
            {
                if (TickHistory.Count is 0)
                    break;

                var entry = TickHistory.Pop();
                Rewind.Discard(entry);
            }
        }

        public void Seek(float time)
        {
            if (State is not TimelineState.Paused)
                throw new Exception($"Can Only Seek When Timeline is Paused");

            var index = IndexTimestamp(time);

            if (index < 0)
                throw new InvalidOperationException($"Invalid Seek Operation");

            AnchorTick = TickHistory[index];
            Rewind.Replicate(AnchorTick);
        }

        /// <summary>
        /// Performs a binary search over the ticks returning the index of the closest tick
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        int IndexTimestamp(float timestamp)
        {
            // If the array is empty
            if (TickHistory.Count == 0)
                return -1;

            int low = 0;
            int high = TickHistory.Count - 1;

            // If target is less than the first element
            if (timestamp <= TickHistory[low].Timestamp)
                return low;

            // If target is more than the last element
            if (timestamp >= TickHistory[high].Timestamp)
                return high;

            while (low <= high)
            {
                var mid = (low + high) / 2;
                var value = TickHistory[mid].Timestamp;

                if (value == timestamp)
                    return mid;

                if (timestamp > value)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            // At this point, low > high. The closest values are array[high] and array[low]
            // Handle bounds
            if (high < 0) return low;
            if (low >= TickHistory.Count) return high;

            // Return the closest one
            return Math.Abs(TickHistory[low].Timestamp - timestamp) < Math.Abs(TickHistory[high].Timestamp - timestamp)
                ? low
                : high;
        }

        void Update()
        {
            switch (State)
            {
                case TimelineState.Live:
                    Capture();
                    break;

                case TimelineState.Paused: break;

                default: throw new NotImplementedException();
            }
        }
        void Capture()
        {
            AnchorTick = RewindTick.Increment(AnchorTick, Time.deltaTime);

            if (TickHistory.IsFull)
            {
                var entry = TickHistory.Dequeue();
                Rewind.Discard(entry);
            }

            while (TickHistory.Count > 0)
            {
                ref var entry = ref TickHistory[0];

                if (entry.Timestamp > AnchorTick.Timestamp - Rewind.MaxDuration)
                    break;

                TickHistory.Dequeue();
                Rewind.Discard(entry);
            }

            Rewind.Capture(AnchorTick);
            TickHistory.Push(AnchorTick);
        }
    }

    void Awake()
    {
        Timeline.SetReference(this);
    }

    void Update()
    {
        OnUpdate?.Invoke();
    }
    public event Action OnUpdate;

    #region Singeleton
    public static RewindSystem Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnLoad()
    {
        var prefab = Resources.Load<GameObject>("Rewind System");

        Instance = Instantiate(prefab).GetComponent<RewindSystem>();

        Instance.name = prefab.name;
        DontDestroyOnLoad(Instance);
    }
    #endregion
}

public enum TimelineState
{
    Live, Paused
}

public struct RewindTick
{
    /// <summary>
    /// Index of tick
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Duration of this one single tick
    /// </summary>
    public float Delta { get; }

    /// <summary>
    /// Timestamp of current tick, basically the accumulation of all previous ticks deltas
    /// </summary>
    public float Timestamp { get; }

    public override string ToString() => $"[Index: {Index} | Delta: {Delta} | Timestamp: {Timestamp}]";

    public RewindTick(int Index, float Delta, float Timestamp)
    {
        this.Index = Index;
        this.Delta = Delta;
        this.Timestamp = Timestamp;
    }

    public static RewindTick Increment(RewindTick data, float deltaTime)
    {
        var index = data.Index + 1;
        var timestamp = data.Timestamp + deltaTime;

        return new RewindTick(index, deltaTime, timestamp);
    }

    public static RewindTick Zero => new RewindTick(0, 0, 0);
    public static RewindTick Max => new RewindTick(int.MaxValue, float.MaxValue, float.MaxValue);
}

public struct RewindCaptureContext
{
    public RewindTick Tick { get; }

    public RewindCaptureContext(RewindTick Tick)
    {
        this.Tick = Tick;
    }
}

public struct RewindDiscardContext
{
    public RewindTick Tick { get; }

    public RewindDiscardContext(RewindTick Tick)
    {
        this.Tick = Tick;
    }
}

public struct RewindPlaybackContext
{
    public RewindTick Tick { get; }

    public RewindPlaybackContext(RewindTick Tick)
    {
        this.Tick = Tick;
    }
}

public struct RewindSimulationContext
{
    public RewindTick Tick { get; }

    public RewindSimulationContext(RewindTick Tick)
    {
        this.Tick = Tick;
    }
}