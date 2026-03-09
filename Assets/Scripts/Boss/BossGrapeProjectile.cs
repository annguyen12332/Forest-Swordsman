using System.Collections;
using UnityEngine;

/// <summary>
/// Boss variant of GrapeProjectile.
/// Can optionally use a custom landing target instead of the player position.
/// Used by BossGrapeShamanAI's Arcane Rain (Phase 3) to place projectiles in a ring.
///
/// If SetTarget() is never called it falls back to tracking the player (same as GrapeProjectile).
///
/// Assign in Inspector:
///   animCurve          → same AnimationCurve as GrapeProjectile (rise then fall)
///   grapeProjectileShadow → shadow sprite prefab
///   splatterPrefab        → GrapeLandSplatter prefab
/// </summary>
public class BossGrapeProjectile : MonoBehaviour
{
    [SerializeField] private float          duration              = 1.2f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float          heightY               = 4f;
    [SerializeField] private GameObject     grapeProjectileShadow;
    [SerializeField] private GameObject     splatterPrefab;

    private Vector3 customTarget;
    private bool    hasCustomTarget;

    /// <summary>Call before Start() to override the landing position (used by Arcane Rain).</summary>
    public void SetTarget(Vector3 target)
    {
        customTarget    = target;
        hasCustomTarget = true;
    }

    private void Start()
    {
        Vector3 targetPos = hasCustomTarget
            ? customTarget
            : PlayerController.Instance.transform.position;

        Vector3 shadowStart = transform.position + Vector3.down * 0.3f;
        GameObject shadow = grapeProjectileShadow != null
            ? Instantiate(grapeProjectileShadow, shadowStart, Quaternion.identity)
            : null;

        StartCoroutine(ArcRoutine(transform.position, targetPos));
        if (shadow != null)
            StartCoroutine(ShadowRoutine(shadow, shadowStart, targetPos));
    }

    private IEnumerator ArcRoutine(Vector3 start, Vector3 end)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linear = elapsed / duration;
            float height = Mathf.Lerp(0f, heightY, animCurve.Evaluate(linear));
            transform.position = Vector2.Lerp(start, end, linear) + Vector2.up * height;
            yield return null;
        }
        if (splatterPrefab != null)
            Instantiate(splatterPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private IEnumerator ShadowRoutine(GameObject shadow, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (shadow == null) yield break;
            shadow.transform.position = Vector2.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        if (shadow != null) Destroy(shadow);
    }
}
