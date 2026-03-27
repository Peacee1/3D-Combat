using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều khiển hiệu ứng khói sân khấu (stage/ground fog).
/// Tạo nhiều luồng khói trải dài mặt đất, lan toả từ từ như fog máy khói.
/// Attach script này vào một empty GameObject, gán smokeMaterial.
/// </summary>
public class StageSmokeController : MonoBehaviour
{
    [Header("Smoke Material")]
    [Tooltip("Gán material khói, ví dụ: Smoke20ss hoặc SmokeBCG")]
    public Material smokeMaterial;

    [Header("Emitter Settings")]
    [Tooltip("Số lượng emitter khói phân bổ theo diện tích")]
    [Range(1, 20)]
    public int emitterCount = 6;

    [Tooltip("Phân bổ emitter trong vùng này (XZ)")]
    public Vector2 emitAreaSize = new Vector2(10f, 6f);

    [Header("Particle Settings")]
    [Tooltip("Số particle mỗi giây mỗi emitter")]
    [Range(1, 30)]
    public float emissionRate = 5f;

    [Tooltip("Thời gian sống của mỗi particle (giây)")]
    public float particleLifetime = 8f;

    [Tooltip("Kích thước bắt đầu của khói")]
    public float startSize = 2f;

    [Tooltip("Kích thước kết thúc (khói nở ra)")]
    public float endSize = 6f;

    [Tooltip("Tốc độ di chuyển lên trên (rất chậm cho khói sân khấu)")]
    public float riseSpeed = 0.1f;

    [Tooltip("Tốc độ trôi ngang (gió nhẹ)")]
    public float driftSpeed = 0.05f;

    [Tooltip("Giới hạn chiều cao khói toả lên")]
    public float heightLimit = 1.5f;

    [Header("Color Settings")]
    [Tooltip("Màu khói (thường trắng hoặc xám nhạt)")]
    public Color smokeColor = new Color(0.9f, 0.9f, 0.95f, 0.6f);

    [Tooltip("Màu cuối vòng đời (khói tan dần)")]
    public Color smokeColorFade = new Color(0.9f, 0.9f, 0.95f, 0f);

    [Header("Control")]
    public bool playOnStart = true;
    public bool isLooping = true;

    private List<ParticleSystem> _emitters = new List<ParticleSystem>();
    private bool _isPlaying = false;

    private void Start()
    {
        CreateEmitters();
        if (playOnStart)
            Play();
    }

    private void CreateEmitters()
    {
        // Xoá emitter cũ nếu có
        foreach (var e in _emitters)
        {
            if (e != null) Destroy(e.gameObject);
        }
        _emitters.Clear();

        for (int i = 0; i < emitterCount; i++)
        {
            // Phân bổ vị trí đều trong vùng
            float t = emitterCount > 1 ? (float)i / (emitterCount - 1) : 0.5f;
            float x = Mathf.Lerp(-emitAreaSize.x * 0.5f, emitAreaSize.x * 0.5f, t);
            // Thêm jitter Z ngẫu nhiên
            float z = Random.Range(-emitAreaSize.y * 0.5f, emitAreaSize.y * 0.5f);

            GameObject emitterGO = new GameObject($"SmokeEmitter_{i}");
            emitterGO.transform.SetParent(transform);
            emitterGO.transform.localPosition = new Vector3(x, 0f, z);

            ParticleSystem ps = emitterGO.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(ps, i);
            _emitters.Add(ps);
        }
    }

    private void ConfigureParticleSystem(ParticleSystem ps, int index)
    {
        // ───── Main module ─────
        var main = ps.main;
        main.loop = isLooping;
        main.playOnAwake = false;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.8f, particleLifetime * 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(riseSpeed * 0.5f, riseSpeed * 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.7f, startSize * 1.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = smokeColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        // ───── Emission module ─────
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate + Random.Range(-1f, 1f); // biến thiên nhẹ

        // ───── Shape module — phun từ disc mỏng sát đất ─────
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.radiusThickness = 1f;

        // ───── Velocity over lifetime — drift ngang + lên ─────
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        // Gió nhẹ theo hướng ngẫu nhiên
        float driftDir = index % 2 == 0 ? 1f : -1f;
        vel.x = new ParticleSystem.MinMaxCurve(driftSpeed * driftDir * 0.5f, driftSpeed * driftDir);
        vel.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.3f, riseSpeed);
        vel.z = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.3f, driftSpeed * 0.3f);

        // ───── Limit velocity (giữ khói không bay quá cao) ─────
        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = heightLimit * 0.2f;
        limitVel.dampen = 0.1f;

        // ───── Size over lifetime — khói nở ra ─────
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, startSize / endSize);
        sizeCurve.AddKey(0.3f, 0.8f);
        sizeCurve.AddKey(1f, 1f);
        sizeOL.size = new ParticleSystem.MinMaxCurve(endSize, sizeCurve);

        // ───── Color over lifetime — fade out cuối ─────
        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(smokeColor, 0f),
                new GradientColorKey(smokeColorFade, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),       // bắt đầu trong suốt
                new GradientAlphaKey(smokeColor.a, 0.1f),  // xuất hiện nhanh
                new GradientAlphaKey(smokeColor.a, 0.7f),  // giữ alpha
                new GradientAlphaKey(0f, 1f)        // tan biến
            }
        );
        colorOL.color = gradient;

        // ───── Rotation over lifetime — xoay từ từ ─────
        var rotOL = ps.rotationOverLifetime;
        rotOL.enabled = true;
        float rotDir = Random.value > 0.5f ? 1f : -1f;
        rotOL.z = new ParticleSystem.MinMaxCurve(
            rotDir * 3f * Mathf.Deg2Rad,
            rotDir * 8f * Mathf.Deg2Rad
        );

        // ───── Noise — làm khói không đều tự nhiên ─────
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;
        noise.octaveCount = 2;

        // ───── Renderer ─────
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        if (smokeMaterial != null)
            renderer.material = smokeMaterial;

        renderer.sortingOrder = 1;

        // ───── Collision — dừng khi chạm đất (không bay xuống đất) ─────
        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.bounce = 0f;
        collision.lifetimeLoss = 0f;
        collision.minKillSpeed = 0f;
        collision.colliderForce = 0f;
    }

    // ───── Public API ─────

    public void Play()
    {
        _isPlaying = true;
        foreach (var ps in _emitters)
        {
            if (ps != null) ps.Play();
        }
    }

    public void Stop()
    {
        _isPlaying = false;
        foreach (var ps in _emitters)
        {
            if (ps != null) ps.Stop();
        }
    }

    public void SetSmokeColor(Color color)
    {
        smokeColor = color;
        // Cập nhật gradient của tất cả emitter
        foreach (var ps in _emitters)
        {
            if (ps == null) continue;
            var colorOL = ps.colorOverLifetime;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(color.a, 0.1f),
                    new GradientAlphaKey(color.a, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOL.color = gradient;
        }
    }

    /// <summary>
    /// Tái tạo toàn bộ emitter (gọi sau khi thay đổi cài đặt trong Inspector)
    /// </summary>
    [ContextMenu("Rebuild Emitters")]
    public void RebuildEmitters()
    {
        bool wasPlaying = _isPlaying;
        Stop();
        CreateEmitters();
        if (wasPlaying) Play();
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vùng emitter trong Scene view
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(emitAreaSize.x, 0.05f, emitAreaSize.y));
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(emitAreaSize.x, heightLimit, emitAreaSize.y));
    }
}
