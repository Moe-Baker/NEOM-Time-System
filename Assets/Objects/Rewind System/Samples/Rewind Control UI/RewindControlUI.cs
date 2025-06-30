using System;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RewindControlUI : MonoBehaviour
{
    [SerializeField]
    GameObject Panel;

    [SerializeField]
    Slider SeekSlider;

    RewindSystem RewindSystem => RewindSystem.Instance;

    void Start()
    {
        RewindSystem.Timeline.OnPause += PauseCallback;
        RewindSystem.Timeline.OnResume += ResumeCallback;

        switch (RewindSystem.Timeline.State)
        {
            case TimelineState.Live:
                ResumeCallback();
                break;

            case TimelineState.Paused:
                PauseCallback();
                break;

            default: throw new NotImplementedException();
        }
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            ToggleTimeline();
    }

    void ToggleTimeline()
    {
        switch (RewindSystem.Timeline.State)
        {
            case TimelineState.Live:
                RewindSystem.Timeline.Pause();
                break;

            case TimelineState.Paused:
                RewindSystem.Timeline.Resume();
                break;
        }
    }

    void PauseCallback()
    {
        Panel.SetActive(true);

        SeekSlider.minValue = RewindSystem.Timeline.MinTime;
        SeekSlider.maxValue = RewindSystem.Timeline.MaxTime;
        SeekSlider.value = RewindSystem.Timeline.MaxTime;

        SeekSlider.onValueChanged.AddListener(Seek);
    }
    void ResumeCallback()
    {
        Panel.SetActive(false);

        SeekSlider.onValueChanged.RemoveListener(Seek);
    }

    void Seek(float time)
    {
        RewindSystem.Timeline.Seek(time);
    }
}