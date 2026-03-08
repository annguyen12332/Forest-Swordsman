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
        Boom,
        Gem
    }

    [SerializeField] private PickupType pickupType;
    [SerializeField] private WeaponInfo boomWeaponInfo;
    [SerializeField] private HeartItemInfo heartItemInfo;
    [SerializeField] private GemItemInfo gemItemInfo;
    [SerializeField] private float pickupDistance = 5f;
    [SerializeField] private float accelerationRate = .2f;
    [SerializeField] private float moveSpeed = 3f;

    private float currentMoveSpeed;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 1.5f;
    [SerializeField] private float popDuration = 1f;

    private Vector3 moveDir;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = 0f;
    }

    private void Start()
    {
        Vector3 endPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0);

        StartCoroutine(AnimCurveSpawnRoutine(transform.position, endPos));
    }

    private void Update()
    {
        if (PlayerController.Instance == null) return;
        Vector3 playerPos = PlayerController.Instance.transform.position;

        if (Vector3.Distance(transform.position, playerPos) < pickupDistance)
        {
            moveDir = (playerPos - transform.position).normalized;
            currentMoveSpeed += accelerationRate;
        }
        else
        {
            moveDir = Vector3.zero;
            currentMoveSpeed = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = (moveSpeed + currentMoveSpeed) * Time.fixedDeltaTime * moveDir;
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
                if (EconomyManager.Instance != null)
                    EconomyManager.Instance.UpdateCurrentGold();
                else
                    Debug.LogError("Pickups: EconomyManager.Instance is null! Make sure EconomyManager is in the scene.");
                break;
            case PickupType.HealthGlobe:
                if (InventoryManager.Instance != null && heartItemInfo != null)
                    InventoryManager.Instance.AddHeartItem(heartItemInfo);
                else if (heartItemInfo == null)
                    Debug.LogWarning("Pickups: heartItemInfo chưa gán trong Inspector!");
                break;
            case PickupType.StaminaGlobe:
                if (Stamina.Instance != null)
                    Stamina.Instance.RefreshStamina();
                break;
            case PickupType.Boom:
                if (ActiveInventory.Instance != null)
                    ActiveInventory.Instance.AddItem(boomWeaponInfo);
                break;
            case PickupType.Gem:
                if (InventoryManager.Instance != null && gemItemInfo != null)
                    InventoryManager.Instance.AddGemItem(gemItemInfo);
                else if (gemItemInfo == null)
                    Debug.LogWarning("Pickups: gemItemInfo chưa gán trong Inspector!");
                break;
            default:
                break;
        }
    }
}
