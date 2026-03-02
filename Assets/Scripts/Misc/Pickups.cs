using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickups : MonoBehaviour
{
    private enum PickupType
    {
        GoldCoin,
        StaminaGlobe,
        HealthGlobe,
        Boom
    }

    [SerializeField] private PickupType pickupType;
    [SerializeField] private WeaponInfo boomWeaponInfo;
    [SerializeField] private float pickupDistance = 5f;
    [SerializeField] private float accelerationRate = .2f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 1.5f;
    [SerializeField] private float popDuration = 1f;

    private Vector3 moveDir;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Vector3 endPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0);

        StartCoroutine(AnimCurveSpawnRoutine(transform.position, endPos));
    }

    private void Update()
    {
        Vector3 playerPos = PlayerController.Instance.transform.position;

        if (Vector3.Distance(transform.position, playerPos) < pickupDistance)
        {
            moveDir = (playerPos - transform.position).normalized;
            moveSpeed += accelerationRate;
        }
        else
        {
            moveDir = Vector3.zero;
            moveSpeed = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null) {
            rb.linearVelocity = moveSpeed * Time.deltaTime * moveDir;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            DetectPickupType();
            Destroy(gameObject);
        }
    }

    private IEnumerator AnimCurveSpawnRoutine(Vector3 startPosition, Vector3 endPosition)
    {
        float timePassed = 0f;

        while (timePassed < popDuration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / popDuration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heightY, heightT);

            transform.position = Vector2.Lerp(startPosition, endPosition, linearT) + new Vector2(0f, height);

            yield return null;
        }
    }

    private void DetectPickupType()
    {
        switch (pickupType)
        {
            case PickupType.GoldCoin:
                EconomyManager.Instance.UpdateCurrentGold();
                break;
            case PickupType.HealthGlobe:
                PlayerHealth.Instance.HealPlayer();
                break;
            case PickupType.StaminaGlobe:
                Stamina.Instance.RefreshStamina();
                break;
            case PickupType.Boom:
                ActiveInventory.Instance.AddItem(boomWeaponInfo);
                break;
            default:
                break;
        }
    }
}
