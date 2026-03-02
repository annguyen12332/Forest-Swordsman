using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomProjectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private int damageAmount = 5;
    [SerializeField] private float projectileScale = 1f;

    private Vector3 startPosition;
    private float timeElapsed = 0f;
    private const float MIN_TIME_TO_EXPLODE = 0.1f;

    private void Start()
    {
        startPosition = transform.position;
        transform.localScale = new Vector3(projectileScale, projectileScale, 1f);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
        MoveProjectile();
        DetectFireDistance();
    }

    public void UpdateProjectileRange(float projectileRange)
    {
        this.projectileRange = projectileRange;
    }

    public void SetDamage(int damage) {
        damageAmount = damage;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (timeElapsed < MIN_TIME_TO_EXPLODE) return;
        if (other.isTrigger) return;

        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        Indestructible indestructible = other.gameObject.GetComponent<Indestructible>();
        PlayerController player = other.gameObject.GetComponent<PlayerController>();

        // Nếu chạm vào Player thì bỏ qua, không nổ
        if (player != null) return;

        if (enemyHealth || indestructible || other.gameObject.CompareTag("Environment"))
        {
            Debug.Log("Boom hit: " + other.gameObject.name);
            Explode();
        }
    }

    private void DetectFireDistance()
    {
        if (Vector3.Distance(transform.position, startPosition) > projectileRange)
        {
            Explode();
        }
    }

    private void MoveProjectile()
    {
        transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
    }

    private void Explode() {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.GetComponent<DamageSource>().SetDamage(damageAmount);
        Destroy(gameObject);
    }
}
