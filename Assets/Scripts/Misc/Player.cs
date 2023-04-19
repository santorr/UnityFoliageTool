using UnityEngine;

public class Player : MonoBehaviour
{
    public float MovementSpeed = 5.0f;
    public float MouseSensitivity = 2.0f;
    public float MaxVerticalAngle = 80.0f;
    public float MinVerticalAngle = -80.0f;

    private CharacterController _characterController;
    private Camera _playerCamera;
    private Vector3 _movementDirection = Vector3.zero;
    private float _verticalRotation = 0.0f;
    private float _verticalVelocity = 0.0f;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerCamera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        _movementDirection = transform.TransformDirection(new Vector3(horizontalInput, 0.0f, verticalInput));
        _movementDirection *= MovementSpeed;

        _verticalVelocity = CalculateGravity(_verticalVelocity);
        _movementDirection.y = _verticalVelocity;

        _characterController.Move(_movementDirection * Time.deltaTime);

        float horizontalMouseInput = Input.GetAxis("Mouse X") * MouseSensitivity;
        transform.Rotate(0.0f, horizontalMouseInput, 0.0f);

        _verticalRotation -= Input.GetAxis("Mouse Y") * MouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, MinVerticalAngle, MaxVerticalAngle);
        _playerCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0.0f, 0.0f);
    }

    private float CalculateGravity(float verticalVelocity)
    {
        if (_characterController.isGrounded)
        {
            verticalVelocity = Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        return verticalVelocity;
    }
}