using UnityEngine;

/// <summary>
/// LoL-style Isometric Camera
/// - Camera đứng cố định góc isometric, follow nhân vật
/// - Hỗ trợ scroll wheel zoom in/out
/// - Hỗ trợ kéo cạnh màn hình để pan (tuỳ chọn)
/// </summary>
public class CameraMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform của nhân vật cần follow")]
    [SerializeField] private Transform target;

    [Header("Isometric Offset")]
    [Tooltip("Offset của camera so với nhân vật (x, y, z)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -8f);

    [Header("Follow Smoothing")]
    [Tooltip("Độ mượt khi camera follow (thấp = mượt hơn)")]
    [SerializeField] private float followSmoothTime = 0.12f;

    [Header("Zoom")]
    [SerializeField] private bool enableZoom = true;
    [Tooltip("Tốc độ zoom bằng scroll wheel")]
    [SerializeField] private float zoomSpeed = 2f;
    [Tooltip("Khoảng cách zoom tối thiểu (y offset)")]
    [SerializeField] private float minZoom = 5f;
    [Tooltip("Khoảng cách zoom tối đa (y offset)")]
    [SerializeField] private float maxZoom = 20f;

    [Header("Edge Pan (tuỳ chọn)")]
    [SerializeField] private bool enableEdgePan = false;
    [Tooltip("Vùng cạnh màn hình kích hoạt pan (pixel)")]
    [SerializeField] private float edgePanThreshold = 20f;
    [Tooltip("Tốc độ pan")]
    [SerializeField] private float edgePanSpeed = 8f;
    [Tooltip("Phạm vi pan tối đa từ vị trí nhân vật")]
    [SerializeField] private float maxPanDistance = 10f;

    // ── Private ──────────────────────────────────────────────────────────────
    private Vector3 _velocity           = Vector3.zero;
    private Vector3 _panOffset          = Vector3.zero;
    private Vector3 _desiredPosition;
    private float   _currentZoom;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _currentZoom = offset.y;
    }

    private void Start()
    {
        if (target == null)
        {
            // Auto-find player nếu chưa assign
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Snap to target ngay lập tức khi bắt đầu
        if (target != null)
            transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleEdgePan();
        FollowTarget();
    }

    // ── Zoom ─────────────────────────────────────────────────────────────────

    private void HandleZoom()
    {
        if (!enableZoom) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _currentZoom -= scroll * zoomSpeed * 10f;
        _currentZoom  = Mathf.Clamp(_currentZoom, minZoom, maxZoom);

        // Scale cả offset.y và offset.z theo tỉ lệ zoom
        float ratio = _currentZoom / offset.y;
        offset = new Vector3(offset.x, _currentZoom, offset.z * ratio);
    }

    // ── Edge Pan ──────────────────────────────────────────────────────────────

    private void HandleEdgePan()
    {
        if (!enableEdgePan) return;

        Vector3 panDelta = Vector3.zero;
        Vector2 mousePos = Input.mousePosition;

        if (mousePos.x < edgePanThreshold)
            panDelta -= transform.right;
        else if (mousePos.x > Screen.width - edgePanThreshold)
            panDelta += transform.right;

        if (mousePos.y < edgePanThreshold)
            panDelta -= Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        else if (mousePos.y > Screen.height - edgePanThreshold)
            panDelta += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        _panOffset += panDelta * edgePanSpeed * Time.deltaTime;
        _panOffset  = Vector3.ClampMagnitude(_panOffset, maxPanDistance);

        // Khi không pan thì drift về 0
        if (panDelta.sqrMagnitude < 0.001f)
            _panOffset = Vector3.Lerp(_panOffset, Vector3.zero, Time.deltaTime * 5f);
    }

    // ── Follow ────────────────────────────────────────────────────────────────

    private void FollowTarget()
    {
        _desiredPosition = target.position + offset + _panOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            _desiredPosition,
            ref _velocity,
            followSmoothTime
        );
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Đổi mục tiêu camera (ví dụ: khi đổi nhân vật).</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>Reset pan offset về nhân vật.</summary>
    public void ResetPan() => _panOffset = Vector3.zero;
}
