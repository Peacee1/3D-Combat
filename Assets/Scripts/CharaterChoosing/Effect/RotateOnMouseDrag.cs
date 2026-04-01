using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Chỉ rotate object khi click TRÚNG object này rồi drag chuột
/// </summary>
public class RotateOnMouseDrag : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private bool invertDirection = false;

    private Vector2 _lastMousePos;
    private bool _isDragging;
    private Mouse _mouse;
    private Camera _mainCamera;

    private void OnEnable()
    {
        _mouse = Mouse.current;
        _mainCamera = Camera.main;
        _isDragging = false;
    }

    private void OnDisable()
    {
        _isDragging = false;
    }

    private void Update()
    {
        if (_mouse == null) { _mouse = Mouse.current; return; }
        if (_mainCamera == null) { _mainCamera = Camera.main; return; }

        // Bắt đầu drag: chỉ khi click trúng object này
        if (_mouse.leftButton.wasPressedThisFrame)
        {
            if (IsClickingThisObject())
            {
                _isDragging = true;
                _lastMousePos = _mouse.position.ReadValue();
            }
        }

        // Kết thúc drag khi nhả chuột
        if (_mouse.leftButton.wasReleasedThisFrame)
        {
            _isDragging = false;
        }

        // Rotate khi đang drag
        if (_isDragging && _mouse.leftButton.isPressed)
        {
            Vector2 currentPos = _mouse.position.ReadValue();
            Vector2 delta = currentPos - _lastMousePos;

            float dir = invertDirection ? 1f : -1f;
            float amount = delta.x * rotationSpeed * dir * Time.deltaTime;
            transform.Rotate(Vector3.up, amount, Space.World);

            _lastMousePos = currentPos;
        }
    }

    private bool IsClickingThisObject()
    {
        Ray ray = _mainCamera.ScreenPointToRay(_mouse.position.ReadValue());

        // Raycast — kiểm tra có trúng collider thuộc object này (hoặc child) không
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider != null && hit.collider.transform.IsChildOf(transform);
        }

        return false;
    }
}
