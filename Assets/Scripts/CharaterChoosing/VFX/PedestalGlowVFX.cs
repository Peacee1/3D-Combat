using UnityEngine;

/// <summary>
/// VFX Pedestal Glow Ring — tạo các vòng sáng toả ra giống đế nhân vật Solo Leveling
/// Attach vào một empty GameObject đặt dưới chân nhân vật
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class PedestalGlowVFX : MonoBehaviour
{
    [Header("Ring Settings")]
    [SerializeField] private int ringCount = 3;
    [SerializeField] private float baseRadius = 0.6f;
    [SerializeField] private float maxRadius = 1.8f;
    [SerializeField] private float ringSpeed = 0.6f;       // tốc độ toả ra
    [SerializeField] private float ringWidth = 0.04f;
    [SerializeField] private int ringSegments = 80;

    [Header("Colors")]
    [SerializeField] private Color innerColor = new Color(1f, 0.92f, 0.5f, 1f);    // vàng sáng
    [SerializeField] private Color outerColor = new Color(1f, 0.7f, 0.1f, 0f);     // vàng mờ (fade out)
    [SerializeField] private float emissiveIntensity = 3f;

    [Header("Center Glow")]
    [SerializeField] private bool showCenterGlow = true;
    [SerializeField] private float centerRadius = 0.55f;
    [SerializeField] private Color centerColor = new Color(1f, 0.95f, 0.6f, 0.25f);

    [Header("Sparkles")]
    [SerializeField] private bool showSparkles = true;
    [SerializeField] private int sparkleCount = 40;
    [SerializeField] private Color sparkleColor = new Color(1f, 0.9f, 0.4f, 1f);

    // Rings
    private LineRenderer[] _rings;
    private float[] _ringPhases;   // offset phase để các vòng không đồng bộ

    // Center disc
    private GameObject _centerDisc;
    private MeshRenderer _centerRenderer;

    // Sparkles
    private ParticleSystem _sparklePS;

    private Material _ringMat;
    private Material _centerMat;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        CreateMaterials();
        CreateRings();
        if (showCenterGlow) CreateCenterDisc();
        if (showSparkles) CreateSparkles();
    }

    private void Update()
    {
        UpdateRings();
    }

    // ── Materials ─────────────────────────────────────────────────────────

    private void CreateMaterials()
    {
        // URP Unlit + Emission để glow
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
            unlitShader = Shader.Find("Unlit/Color");

        _ringMat = new Material(unlitShader);
        _ringMat.SetColor("_BaseColor", innerColor);
        _ringMat.EnableKeyword("_EMISSION");
        _ringMat.SetColor("_EmissionColor", innerColor * emissiveIntensity);
        _ringMat.renderQueue = 3000;

        _centerMat = new Material(unlitShader);
        _centerMat.SetColor("_BaseColor", centerColor);
        EnableTransparency(_centerMat);
    }

    private void EnableTransparency(Material mat)
    {
        mat.SetFloat("_Surface", 1f);       // Transparent
        mat.SetFloat("_Blend", 0f);         // Alpha
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
    }

    // ── Rings ─────────────────────────────────────────────────────────────

    private void CreateRings()
    {
        _rings = new LineRenderer[ringCount];
        _ringPhases = new float[ringCount];

        for (int i = 0; i < ringCount; i++)
        {
            var go = new GameObject($"Ring_{i}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.positionCount = ringSegments + 1;
            lr.startWidth = ringWidth;
            lr.endWidth = ringWidth;
            lr.material = new Material(_ringMat);
            lr.useWorldSpace = false;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            // Offset phase đều nhau
            _ringPhases[i] = (float)i / ringCount;
            _rings[i] = lr;

            SetRingRadius(lr, baseRadius, 1f);
        }
    }

    private void UpdateRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            // t đi từ 0 → 1 liên tục
            float t = (_ringPhases[i] + Time.time * ringSpeed) % 1f;

            float radius = Mathf.Lerp(baseRadius, maxRadius, t);
            float alpha = 1f - t; // fade out khi ra ngoài

            // Màu fade
            Color col = Color.Lerp(innerColor, outerColor, t);
            col.a = Mathf.Clamp01(alpha);

            _rings[i].material.SetColor("_BaseColor", col);
            _rings[i].material.SetColor("_EmissionColor", col * emissiveIntensity * (1f - t));

            SetRingRadius(_rings[i], radius, alpha);
        }
    }

    private void SetRingRadius(LineRenderer lr, float radius, float alpha)
    {
        for (int j = 0; j <= ringSegments; j++)
        {
            float angle = j * 2f * Mathf.PI / ringSegments;
            lr.SetPosition(j, new Vector3(
                Mathf.Cos(angle) * radius,
                0.002f, // nhô lên mặt đất chút
                Mathf.Sin(angle) * radius
            ));
        }

        float w = ringWidth * Mathf.Lerp(1.5f, 0.5f, 1f - alpha);
        lr.startWidth = w;
        lr.endWidth = w;
    }

    // ── Center Disc ───────────────────────────────────────────────────────

    private void CreateCenterDisc()
    {
        _centerDisc = new GameObject("CenterDisc");
        _centerDisc.transform.SetParent(transform, false);
        _centerDisc.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        _centerDisc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var mesh = CreateCircleMesh(centerRadius, 64);
        var mf = _centerDisc.AddComponent<MeshFilter>();
        mf.mesh = mesh;

        _centerRenderer = _centerDisc.AddComponent<MeshRenderer>();
        _centerRenderer.material = _centerMat;
        _centerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _centerRenderer.receiveShadows = false;
    }

    private Mesh CreateCircleMesh(float radius, int segments)
    {
        var mesh = new Mesh();
        var verts = new Vector3[segments + 1];
        var tris = new int[segments * 3];
        var uvs = new Vector2[segments + 1];

        verts[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            uvs[i + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f);

            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }

    // ── Sparkles ──────────────────────────────────────────────────────────

    private void CreateSparkles()
    {
        var go = new GameObject("Sparkles");
        go.transform.SetParent(transform, false);

        _sparklePS = go.AddComponent<ParticleSystem>();

        var main = _sparklePS.main;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = sparkleColor;
        main.gravityModifier = -0.05f; // nhẹ bay lên
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = sparkleCount;

        var emission = _sparklePS.emission;
        emission.rateOverTime = sparkleCount / 2f;

        var shape = _sparklePS.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = baseRadius * 0.9f;
        shape.radiusThickness = 0.3f;

        // Fade out cuối đời
        var colorOverLife = _sparklePS.colorOverLifetime;
        colorOverLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(sparkleColor, 0f),
                new GradientColorKey(sparkleColor, 0.7f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = grad;

        // Renderer
        var renderer = _sparklePS.GetComponent<ParticleSystemRenderer>();
        renderer.material = _ringMat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 1;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Đổi màu toàn bộ vfx (ví dụ: chuyển vàng → tím khi selected)</summary>
    public void SetColor(Color color)
    {
        innerColor = color;
        outerColor = new Color(color.r, color.g, color.b, 0f);
        sparkleColor = color;

        if (_centerMat) _centerMat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, 0.25f));
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
