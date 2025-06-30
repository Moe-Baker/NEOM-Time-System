using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField]
    InputActionAsset InputAsset;

    [SerializeField]
    Camera Camera;

    Rigidbody Rigidbody;

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        InputAsset.Enable();
    }
    void OnDisable()
    {
        InputAsset.Disable();
    }

    void Update()
    {
        Move();
        Look();
    }

    #region Move
    [SerializeField]
    float MoveSpeed;

    [SerializeField]
    float MoveAcceleration;

    Vector3 MoveVelocity;

    void Move()
    {
        var input = InputAsset["Player/Move"].ReadValue<Vector2>();

        var target = (transform.forward * input.y) + (transform.right * input.x);
        target *= MoveSpeed;
        target = Vector3.ClampMagnitude(target, MoveSpeed);

        MoveVelocity = Vector3.MoveTowards(MoveVelocity, target, MoveAcceleration * Time.deltaTime);
        Rigidbody.linearVelocity = MoveVelocity;
    }
    #endregion

    #region Look
    [SerializeField]
    float LookSensitivity;

    void Look()
    {
        var input = InputAsset["Player/Look"].ReadValue<Vector2>();

        //Horizontal - Body
        {
            transform.localRotation *= Quaternion.Euler(Vector3.up * input.x * LookSensitivity * Time.deltaTime);
        }

        //Vertical - Camera
        {
            Camera.transform.localRotation *= Quaternion.Euler(Vector3.left * input.y * LookSensitivity * Time.deltaTime);
        }
    }
    #endregion
}