using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RewindSystem : MonoBehaviour
{

    [SerializeField, Tooltip("Max Desired FPS")]
    int MaxFPS = 60;

    [SerializeField, Tooltip("Max Recording Duration in Seconds")]
    float MaxDuration = 30;

    public int MaxSnapshotsCapacity => Mathf.CeilToInt(MaxFPS * MaxDuration);

    void Capture(int tick)
    {
        var context = new RewindCaptureContext(tick);
        OnCapture?.Invoke(context);
    }
    public event CaptureContextDelegate OnCapture;
    public delegate void CaptureContextDelegate(RewindCaptureContext context);

    void Replicate(int tick)
    {
        var context = new RewindPlaybackContext(tick);
        OnReplicate(context);
    }
    public event PlaybackContextDelegate OnReplicate;
    public delegate void PlaybackContextDelegate(RewindPlaybackContext context);

    void Simulate(int tick)
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
        TickData Anchor;
        RingBuffer<TickData> Ticks;
        public struct TickData
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

            public TickData(int Index, float Delta, float Timestamp)
            {
                this.Index = Index;
                this.Delta = Delta;
                this.Timestamp = Timestamp;
            }

            public static TickData Increment(TickData data, float deltaTime)
            {
                var index = data.Index + 1;
                var timestamp = data.Timestamp + deltaTime;

                return new TickData(index, deltaTime, timestamp);
            }
        }

        public int TickCount => Ticks.Count;

        public float MaxTime => Anchor.Timestamp;
        public float MinTime => Mathf.Max(Ticks[0].Timestamp, MaxTime - Rewind.MaxDuration);

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

            Anchor = new TickData(-1, 0f, 0f);
            Ticks = new RingBuffer<TickData>(Rewind.MaxSnapshotsCapacity);

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
            //Remove discarded ticks
            while (Ticks.Count > 0)
            {
                ref var entry = ref Ticks[^1];

                if (entry.Index <= Anchor.Index)
                    break;

                Ticks.Pop();
            }

            Rewind.Simulate(Anchor.Index);
        }

        public void Seek(float time)
        {
            if (State is not TimelineState.Paused)
                throw new Exception($"Can Only Seek When Timeline is Paused");

            var index = IndexTimestamp(time);

            if (index < 0)
                throw new InvalidOperationException($"Invalid Seek Operation");

            Anchor = Ticks[index];
            Rewind.Replicate(Anchor.Index);
        }

        /// <summary>
        /// Performs a binary search over the ticks returning the index of the closest tick
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        int IndexTimestamp(float timestamp)
        {
            // If the array is empty
            if (Ticks.Count == 0)
                return -1;

            int low = 0;
            int high = Ticks.Count - 1;

            // If target is less than the first element
            if (timestamp <= Ticks[low].Timestamp)
                return low;

            // If target is more than the last element
            if (timestamp >= Ticks[high].Timestamp)
                return high;

            while (low <= high)
            {
                var mid = (low + high) / 2;
                var value = Ticks[mid].Timestamp;

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
            if (low >= Ticks.Count) return high;

            // Return the closest one
            return Math.Abs(Ticks[low].Timestamp - timestamp) < Math.Abs(Ticks[high].Timestamp - timestamp)
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
            Anchor = TickData.Increment(Anchor, Time.deltaTime);

            Rewind.Capture(Anchor.Index);

            Ticks.Push(Anchor);
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

public struct RewindCaptureContext
{
    public int Tick { get; }

    public RewindCaptureContext(int Tick)
    {
        this.Tick = Tick;
    }
}

public struct RewindPlaybackContext
{
    public int Tick { get; }

    public RewindPlaybackContext(int Tick)
    {
        this.Tick = Tick;
    }
}

public struct RewindSimulationContext
{
    public int Tick { get; }

    public RewindSimulationContext(int Tick)
    {
        this.Tick = Tick;
    }
}