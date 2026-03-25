using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;

/// <summary>
/// Quản lý scene chọn nhân vật.
/// - Vòng tròn đi qua điểm (0, 0.5, 0) là vị trí nhân vật được chọn (selectedAngle = 270°)
/// - Mặc định: Nam ở center (270°), Nữ ở phải-sau (unselectedAngle)
/// - Click nhân vật → trượt theo cung tròn đến vị trí đối phương
/// - choosingCharacter: bật RotateOnMouseDrag | còn lại: tắt và luôn nhìn về camera
/// </summary>
public class InputHandleForCharacterPickingScene : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private GameObject maleCharacter;
    [SerializeField] private GameObject femaleCharacter;

    [Header("Orbit Settings")]
    [Tooltip("Đặt radius, center tự tính để đường tròn đi qua (0, 0.5, 0) tại selectedAngle")]
    [SerializeField] private float orbitRadius = 2f;

    [Tooltip("Góc (°) của vị trí được chọn — đường tròn sẽ đi qua (0, 0.5, 0) tại đây")]
    [SerializeField] private float selectedAngle = 270f;

    [Tooltip("Góc (°) của nhân vật không được chọn (phải-sau từ góc cam)")]
    [SerializeField] private float unselectedAngle = 330f;

    [Tooltip("Chiều cao Y của toàn bộ chuyển động")]
    [SerializeField] private float orbitY = 0.5f;

    [Header("Tween Settings")]
    [SerializeField] private float moveDuration = 0.7f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    [Header("Face Camera")]
    [SerializeField] private float faceSpeed = 6f;

    [Header("Name Labels")]
    [Tooltip("Khoảng cách label so với vị trí nhân vật")]
    [SerializeField] private float labelOffsetY = 0.5f;
    [SerializeField] private float labelFontSize = 2f;
    [SerializeField] private Color selectedLabelColor = new Color(1f, 0.85f, 0.2f);   // vàng
    [SerializeField] private Color unselectedLabelColor = new Color(0.7f, 0.7f, 0.7f); // xám

    // ── State ─────────────────────────────────────────────────────────────
    private GameObject _choosingCharacter;
    private Camera _mainCamera;
    private Mouse _mouse;

    private float _maleAngle;
    private float _femaleAngle;

    private Tween _maleTween;
    private Tween _femaleTween;

    private bool _isSwitching;

    // Tâm vòng tròn — tính để đường tròn đi qua (0, 0.5, 0) tại selectedAngle
    private Vector3 _orbitCenter;

    // Labels
    private TextMeshPro _maleLabel;
    private TextMeshPro _femaleLabel;

    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Tính tâm vòng tròn sao cho điểm tại selectedAngle = (0, orbitY, 0)
        // pos = center + r*(cos, 0, sin)  →  center = (0,orbitY,0) - r*(cos, 0, sin)
        float rad = selectedAngle * Mathf.Deg2Rad;
        _orbitCenter = new Vector3(
            0f - orbitRadius * Mathf.Cos(rad),
            orbitY,
            0f - orbitRadius * Mathf.Sin(rad)
        );

        // Đặt vị trí ban đầu
        _maleAngle   = selectedAngle;
        _femaleAngle = unselectedAngle;

        if (maleCharacter)   maleCharacter.transform.position   = GetPositionOnOrbit(_maleAngle);
        if (femaleCharacter) femaleCharacter.transform.position = GetPositionOnOrbit(_femaleAngle);

        // Nam được chọn mặc định
        SetChoosingCharacter(maleCharacter, instant: true);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _mouse = Mouse.current;

        // Tạo label
        _maleLabel   = CreateLabel(maleCharacter,   "MALE");
        _femaleLabel = CreateLabel(femaleCharacter, "FEMALE");

        // Snap tất cả nhân vật về hướng camera ngay khi vào scene
        FaceCameraInstant(maleCharacter);
        FaceCameraInstant(femaleCharacter);

        // Màu label ban đầu
        UpdateLabelColors();
    }

    private void Update()
    {
        if (_mouse == null) { _mouse = Mouse.current; return; }

        if (!_isSwitching && _mouse.leftButton.wasPressedThisFrame)
            HandleClick();

        // Nhân vật không được chọn luôn nhìn về camera
        FaceCamera(_choosingCharacter == maleCharacter ? femaleCharacter : maleCharacter);

        // Labels luôn nhìn về camera (billboard)
        BillboardLabel(_maleLabel,   maleCharacter);
        BillboardLabel(_femaleLabel, femaleCharacter);
    }

    // ── Click Detection ───────────────────────────────────────────────────

    private void HandleClick()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(_mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        GameObject clickedRoot = GetCharacterRoot(hit.collider.gameObject);
        if (clickedRoot == null || clickedRoot == _choosingCharacter) return;

        SwitchCharacter(clickedRoot);
    }

    // ── Switch Logic ──────────────────────────────────────────────────────

    private void SwitchCharacter(GameObject newChoosing)
    {
        _isSwitching = true;

        GameObject previous = _choosingCharacter;

        // Tắt RotateOnMouseDrag ngay cho nhân vật sắp bị bỏ chọn
        SetRotateDrag(previous, false);

        float fromAngle, toAngle, prevFrom, prevTo;

        if (newChoosing == maleCharacter)
        {
            // Male: unselectedAngle → selectedAngle  (vào từ phải)
            fromAngle = _maleAngle;
            toAngle   = selectedAngle;
            prevFrom  = _femaleAngle;
            prevTo    = unselectedAngle;
        }
        else
        {
            // Female: unselectedAngle → selectedAngle (vào từ phải)
            fromAngle = _femaleAngle;
            toAngle   = selectedAngle;
            prevFrom  = _maleAngle;
            prevTo    = unselectedAngle;
        }

        // Kill tweens cũ
        _maleTween?.Kill();
        _femaleTween?.Kill();

        bool done1 = false, done2 = false;

        Tween incomingTween = AnimateAlongArc(
            newChoosing, fromAngle, toAngle,
            angle =>
            {
                if (newChoosing == maleCharacter) _maleAngle = angle;
                else _femaleAngle = angle;
            },
            onComplete: () =>
            {
                done1 = true;
                if (done1 && done2) FinishSwitch(newChoosing);
            }
        );

        Tween outgoingTween = AnimateAlongArc(
            previous, prevFrom, prevTo,
            angle =>
            {
                if (previous == maleCharacter) _maleAngle = angle;
                else _femaleAngle = angle;
            },
            onComplete: () =>
            {
                done2 = true;
                if (done1 && done2) FinishSwitch(newChoosing);
            }
        );

        if (newChoosing == maleCharacter) { _maleTween = incomingTween; _femaleTween = outgoingTween; }
        else                             { _femaleTween = incomingTween; _maleTween = outgoingTween; }
    }

    private void FinishSwitch(GameObject newChoosing)
    {
        SetChoosingCharacter(newChoosing, instant: false);
        _isSwitching = false;
        UpdateLabelColors();

        Debug.Log($"[CharacterPicking] Selected: {_choosingCharacter.name}");
    }

    // ── Arc Animation ─────────────────────────────────────────────────────

    /// <summary>
    /// Trượt nhân vật theo cung tròn từ fromAngle → toAngle.
    /// Luôn đi theo chiều GIẢM góc (clockwise từ trên nhìn xuống)
    /// → nhân vật vào từ phía PHẢI và trượt sang TRÁI tới center.
    /// </summary>
    private Tween AnimateAlongArc(
        GameObject character,
        float fromAngle,
        float toAngle,
        System.Action<float> onAngleUpdate,
        System.Action onComplete)
    {
        // Đảm bảo luôn đi theo chiều giảm góc (clockwise)
        // Nếu toAngle >= fromAngle thì nhân toAngle = toAngle - 360 để đi vòng ngược chiều kim đồng hồ
        float adjustedTo = toAngle;
        while (adjustedTo >= fromAngle) adjustedTo -= 360f;

        return DOVirtual
            .Float(fromAngle, adjustedTo, moveDuration,
                t =>
                {
                    float normalised = ((t % 360f) + 360f) % 360f;
                    onAngleUpdate?.Invoke(normalised);
                    character.transform.position = GetPositionOnOrbit(normalised);
                })
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                // Snap chính xác
                float finalNorm = ((toAngle % 360f) + 360f) % 360f;
                onAngleUpdate?.Invoke(finalNorm);
                character.transform.position = GetPositionOnOrbit(finalNorm);
                onComplete?.Invoke();
            });
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetChoosingCharacter(GameObject newChoosing, bool instant)
    {
        _choosingCharacter = newChoosing;
        SetRotateDrag(maleCharacter,   maleCharacter   == _choosingCharacter);
        SetRotateDrag(femaleCharacter, femaleCharacter == _choosingCharacter);
    }

    private void SetRotateDrag(GameObject character, bool enabled)
    {
        if (character == null) return;
        var drag = character.GetComponentInChildren<RotateOnMouseDrag>(includeInactive: true);
        if (drag != null) drag.enabled = enabled;
    }

    private void FaceCamera(GameObject character)
    {
        if (character == null || _mainCamera == null || _isSwitching) return;

        Vector3 dir = _mainCamera.transform.position - character.transform.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion target = Quaternion.LookRotation(dir);
        character.transform.rotation = Quaternion.Slerp(
            character.transform.rotation, target, faceSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Snap ngay lập tức về hướng camera — dùng khi khởi động scene
    /// </summary>
    private void FaceCameraInstant(GameObject character)
    {
        if (character == null || _mainCamera == null) return;

        Vector3 dir = _mainCamera.transform.position - character.transform.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        character.transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── Label Helpers ─────────────────────────────────────────────────────

    private TextMeshPro CreateLabel(GameObject character, string text)
    {
        if (character == null) return null;

        // Tạo GameObject con gắn vào character
        var labelObj = new GameObject($"Label_{text}");
        labelObj.transform.SetParent(character.transform, worldPositionStays: false);
        labelObj.transform.localPosition = new Vector3(0f, labelOffsetY, 0f);
        labelObj.transform.localRotation = Quaternion.identity;

        var tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = labelFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = unselectedLabelColor;

        return tmp;
    }

    private void BillboardLabel(TextMeshPro label, GameObject character)
    {
        if (label == null || _mainCamera == null || character == null) return;

        // Cập nhật vị trí theo nhân vật (đầu nhân vật)
        label.transform.position = character.transform.position + Vector3.up * labelOffsetY;

        // Luôn nhìn về phía camera (billboard)
        label.transform.rotation = Quaternion.LookRotation(
            label.transform.position - _mainCamera.transform.position);
    }

    private void UpdateLabelColors()
    {
        if (_maleLabel != null)
            _maleLabel.color   = (_choosingCharacter == maleCharacter)   ? selectedLabelColor : unselectedLabelColor;
        if (_femaleLabel != null)
            _femaleLabel.color = (_choosingCharacter == femaleCharacter) ? selectedLabelColor : unselectedLabelColor;
    }

    private Vector3 GetPositionOnOrbit(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(
            _orbitCenter.x + orbitRadius * Mathf.Cos(rad),
            orbitY,
            _orbitCenter.z + orbitRadius * Mathf.Sin(rad)
        );
    }

    private GameObject GetCharacterRoot(GameObject clicked)
    {
        Transform t = clicked.transform;
        while (t != null)
        {
            if (t.gameObject == maleCharacter)   return maleCharacter;
            if (t.gameObject == femaleCharacter) return femaleCharacter;
            t = t.parent;
        }
        return null;
    }

    public GameObject GetChoosingCharacter() => _choosingCharacter;

    /// <summary>
    /// Gọi từ UI: 0 = male, 1 = female
    /// </summary>
    public void SwitchByIndex(int index)
    {
        GameObject target = (index == 0) ? maleCharacter : femaleCharacter;
        if (target == null || target == _choosingCharacter) return;
        SwitchCharacter(target);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float rad    = selectedAngle * Mathf.Deg2Rad;
        Vector3 center = new Vector3(
            0f - orbitRadius * Mathf.Cos(rad),
            orbitY,
            0f - orbitRadius * Mathf.Sin(rad));

        // Vẽ vòng tròn orbit
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        int seg = 64;
        Vector3 prev = center + new Vector3(orbitRadius, 0, 0);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * 360f / seg * Mathf.Deg2Rad;
            Vector3 next = new Vector3(
                center.x + orbitRadius * Mathf.Cos(a),
                orbitY,
                center.z + orbitRadius * Mathf.Sin(a));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Vị trí selected = (0, orbitY, 0)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(new Vector3(0, orbitY, 0), 0.12f);

        // Vị trí unselected
        float ur = unselectedAngle * Mathf.Deg2Rad;
        Vector3 unselPos = new Vector3(
            center.x + orbitRadius * Mathf.Cos(ur),
            orbitY,
            center.z + orbitRadius * Mathf.Sin(ur));
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(unselPos, 0.12f);

        // Tâm vòng tròn
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.08f);
    }
#endif
}
