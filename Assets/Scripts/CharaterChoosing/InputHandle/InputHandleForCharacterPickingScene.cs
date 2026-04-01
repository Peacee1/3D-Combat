using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandleForCharacterPickingScene : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private GameObject maleCharacter;
    [SerializeField] private GameObject femaleCharacter;

    [Header("Marker Settings")]
    [Tooltip("Đường dẫn prefab trong Resources (không cần .prefab)")]
    [SerializeField] private string markerPrefabPath = "VFX/Marker 2 Pointer Loop";
    [SerializeField] private float markerOffsetY = 2f;

    // ── State ─────────────────────────────────────────────────────────────
    private Camera _mainCamera;
    private Mouse _mouse;
    private GameObject _markerInstance;

    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _mainCamera = Camera.main;
        _mouse = Mouse.current;
    }

    private void Update()
    {
        if (_mouse == null) { _mouse = Mouse.current; return; }

        if (_mouse.leftButton.wasPressedThisFrame)
            HandleClick();
    }

    // ── Click Detection ───────────────────────────────────────────────────

    private void HandleClick()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(_mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        GameObject clickedRoot = GetCharacterRoot(hit.collider.gameObject);
        if (clickedRoot == null) return;

        PlaceMarkerAbove(clickedRoot);
    }

    // ── Marker ────────────────────────────────────────────────────────────

    private void PlaceMarkerAbove(GameObject character)
    {
        // Tạo marker nếu chưa có
        if (_markerInstance == null)
        {
            var prefab = Resources.Load<GameObject>(markerPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CharacterPicking] Không tìm thấy prefab tại Resources/{markerPrefabPath}");
                return;
            }
            _markerInstance = Instantiate(prefab);
        }

        // Đặt vị trí cao hơn character 2 đơn vị Y
        Vector3 targetPos = character.transform.position + Vector3.up * markerOffsetY;
        _markerInstance.transform.position = targetPos;
    }

    // ── Helper ────────────────────────────────────────────────────────────

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
}
