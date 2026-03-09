using System.Collections;
using UnityEngine;

/// <summary>
/// Short-lived AoE ring spawned when Slime King performs a Ground Slam.
/// Attach a CircleCollider2D (IsTrigger = true) and a brief scale animation.
/// Deals damage + knockback to player if they stand in the ring.
/// Self-destroys after lifetime expires.
/// </summary>
public class BossGroundSlamAoE : MonoBehaviour
{
    [SerializeField] private int   damage   = 2;
    [SerializeField] private float lifetime = 0.35f;
    [SerializeField] private float maxScale = 3.5f;

    private bool playerHit; // only hit player once per slam

    private void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale   = Vector3.one * maxScale;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / lifetime);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerHit) return;
        if (other.TryGetComponent(out PlayerHealth ph))
        {
            playerHit = true;
            ph.TakeDamage(damage, transform);
        }
    }
}
