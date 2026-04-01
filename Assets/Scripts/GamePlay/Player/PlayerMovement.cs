using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// LoL-style Click-to-Move Player Movement
/// - Right-click (hoặc Left-click) để nhân vật tự đi đến vị trí trên plane
/// - Sử dụng NavMeshAgent để pathfinding tự động
/// - Body xoay mượt về hướng di chuyển
/// - Có click indicator (VFX tại điểm click)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Tốc độ di chuyển (agent speed)")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Tốc độ xoay nhân vật về hướng di chuyển")]
    [SerializeField] private float rotationSpeed = 720f;

    [Tooltip("Khoảng cách dừng trước đích")]
    [SerializeField] private float stoppingDistance = 0.15f;

    [Header("Input Settings")]
    [Tooltip("Nút chuột để ra lệnh di chuyển (0=Left, 1=Right)")]
    [SerializeField] private int moveMouseButton = 1;

    [Tooltip("Layer của ground/plane để raycast")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Click Indicator")]
    [Tooltip("Prefab hiện tại điểm click (tuỳ chọn)")]
    [SerializeField] private GameObject clickIndicatorPrefab;

    [Tooltip("Thời gian hiện click indicator")]
    [SerializeField] private float indicatorDuration = 0.5f;

    // ── Private ──────────────────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private Camera       _mainCamera;
    private GameObject   _indicatorInstance;
    private Animator     _animator;

    // Animator hashes (tuỳ chọn – chỉ dùng nếu có Animator)
    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        // Apply settings to agent
        _agent.speed           = moveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.angularSpeed    = 0f;   // Tự handle rotation để mượt hơn
        _agent.updateRotation  = false;

        _animator = GetComponent<Animator>(); // Nullable – OK nếu không có
    }

    private void Start()
    {
        _mainCamera = Camera.main;

        // Tạo indicator instance nếu có prefab
        if (clickIndicatorPrefab != null)
        {
            _indicatorInstance = Instantiate(clickIndicatorPrefab);
            _indicatorInstance.SetActive(false);
        }
    }

    private void Update()
    {
        HandleClickInput();
        SmoothRotation();
        UpdateAnimator();
    }

    // ── Input ────────────────────────────────────────────────────────────────

    private void HandleClickInput()
    {
        if (!Input.GetMouseButtonDown(moveMouseButton)) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            MoveTo(hit.point);
            ShowClickIndicator(hit.point, hit.normal);
        }
    }

    // ── Movement ─────────────────────────────────────────────────────────────

    /// <summary>Gọi từ bên ngoài (ví dụ: AI, skill) để ra lệnh di chuyển.</summary>
    public void MoveTo(Vector3 destination)
    {
        if (_agent.isOnNavMesh)
            _agent.SetDestination(destination);
    }

    /// <summary>Dừng nhân vật ngay lập tức.</summary>
    public void StopMoving()
    {
        if (_agent.isOnNavMesh)
            _agent.ResetPath();
    }

    // ── Rotation ─────────────────────────────────────────────────────────────

    private void SmoothRotation()
    {
        if (_agent.velocity.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(_agent.velocity.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    // ── Animator ─────────────────────────────────────────────────────────────

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        float speed = _agent.velocity.magnitude / moveSpeed; // 0-1 normalized
        _animator.SetFloat(HashSpeed, speed, 0.1f, Time.deltaTime);
    }

    // ── Click Indicator ───────────────────────────────────────────────────────

    private void ShowClickIndicator(Vector3 position, Vector3 normal)
    {
        if (_indicatorInstance == null) return;

        _indicatorInstance.transform.position = position + normal * 0.01f;
        _indicatorInstance.transform.rotation = Quaternion.LookRotation(
            Vector3.forward, normal
        );
        _indicatorInstance.SetActive(true);

        CancelInvoke(nameof(HideIndicator));
        Invoke(nameof(HideIndicator), indicatorDuration);
    }

    private void HideIndicator()
    {
        if (_indicatorInstance != null)
            _indicatorInstance.SetActive(false);
    }

    // ── Public Properties ─────────────────────────────────────────────────────

    /// <summary>Nhân vật có đang di chuyển không?</summary>
    public bool IsMoving => _agent.velocity.sqrMagnitude > 0.01f;

    /// <summary>Nhân vật đã đến đích chưa?</summary>
    public bool HasReachedDestination =>
        !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
}
