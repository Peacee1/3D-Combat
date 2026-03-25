using UnityEngine;
using UnityEngine.InputSystem;

public class RotateOnMouseDrag : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private bool invertDirection = false;
    [SerializeField] private bool requireMouseHold = true; // false = luôn quay khi di chuột

    private Vector2 _lastMousePos;
    private bool _isDragging;
    private Mouse _mouse;

    private void OnEnable()
    {
        _mouse = Mouse.current;
    }

    private void Update()
    {
        if (_mouse == null)
        {
            _mouse = Mouse.current;
            return;
        }

        if (requireMouseHold)
        {
            // Chỉ quay khi giữ chuột trái
            if (_mouse.leftButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _lastMousePos = _mouse.position.ReadValue();
            }

            if (_mouse.leftButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                RotateByMouse();
            }
        }
        else
        {
            // Luôn quay khi di chuột (không cần giữ)
            RotateByMouse();
        }
    }

    private void RotateByMouse()
    {
        Vector2 currentMousePos = _mouse.position.ReadValue();
        Vector2 delta = currentMousePos - _lastMousePos;

        float direction = invertDirection ? 1f : -1f;
        float rotationAmount = delta.x * rotationSpeed * direction * Time.deltaTime;

        transform.Rotate(Vector3.up, rotationAmount, Space.World);

        _lastMousePos = currentMousePos;
    }
}
