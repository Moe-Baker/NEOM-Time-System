using UnityEngine;
using UnityEngine.InputSystem;

public class RewindBallLauncher : MonoBehaviour
{
    [SerializeField]
    GameObject Prefab;

    [SerializeField]
    Camera Camera;

    [SerializeField]
    float ForceValue = 20;
    [SerializeField]
    ForceMode ForceMode = ForceMode.VelocityChange;

    RewindSystem RewindSystem => RewindSystem.Instance;

    void Start()
    {
        Camera ??= Camera.main;
    }

    void Update()
    {
        if (RewindSystem.Timeline.State is TimelineState.Paused)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Launch(Mouse.current.position.value);
        }
    }

    int index;
    void Launch(Vector2 screenPoint)
    {
        var ray = Camera.ScreenPointToRay(screenPoint);

        var instance = Instantiate(Prefab, ray.origin, Camera.transform.rotation).GetComponent<BallProjectile>();

        instance.name = $"{Prefab.name} ({index})";
        instance.Rigidbody.AddForce(ray.direction * ForceValue, ForceMode);
    }
}
