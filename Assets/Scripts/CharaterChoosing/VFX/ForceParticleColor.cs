using UnityEngine;

/// <summary>
/// Force màu tím lên toàn bộ Particle System trong prefab lúc runtime
/// Attach vào root GameObject của dusty VFX prefab
/// </summary>
public class ForceParticleColor : MonoBehaviour
{
    [Header("Target Color")]
    [SerializeField] private Color smokeColor = new Color(0.22f, 0.06f, 0.48f, 0.85f);
    [SerializeField] private Color smokeColorMax = new Color(0.32f, 0.10f, 0.62f, 0.92f);

    [Header("Density")]
    [SerializeField] private float emissionMultiplier = 4f;
    [SerializeField] private float alphaMultiplier = 1f;

    private void Start()
    {
        ApplyToAllParticleSystems();
    }

    [ContextMenu("Apply Color Now")]
    public void ApplyToAllParticleSystems()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);

        foreach (var ps in systems)
        {
            var main = ps.main;

            // Đổi startColor sang tím
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(smokeColor.r, smokeColor.g, smokeColor.b, smokeColor.a * alphaMultiplier),
                new Color(smokeColorMax.r, smokeColorMax.g, smokeColorMax.b, smokeColorMax.a * alphaMultiplier)
            );

            // Tăng emission rate
            var emission = ps.emission;
            if (emission.enabled)
            {
                var rate = emission.rateOverTime;
                emission.rateOverTime = rate.constant * emissionMultiplier;
            }

            // Force material sang màu tím
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var mat = new Material(renderer.sharedMaterial);
                mat.SetColor("_BaseColor", smokeColor);
                mat.SetColor("_Color", smokeColor);
                mat.SetColor("_TintColor", smokeColor);
                renderer.material = mat;
            }
        }

        Debug.Log($"[ForceParticleColor] Applied purple to {systems.Length} particle systems");
    }
}
