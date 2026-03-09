using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goldCoin, healthGlobe, staminaGlobe, boomPickup;
    [SerializeField] private GameObject gemPickup;

    [SerializeField] [Range(0, 100)] private int boomDropChance = 10;
    [SerializeField] [Range(0, 100)] private int gemDropChance = 30;

    public void DropItems()
    {
        int randomNum = Random.Range(1, 5);

        if (randomNum == 1)
        {
            Instantiate(healthGlobe, transform.position, Quaternion.identity);
        }
        else if (randomNum == 2)
        {
            Instantiate(staminaGlobe, transform.position, Quaternion.identity);
        }
        else if (randomNum == 3)
        {
            int randomAmountOfGold = Random.Range(1, 4);

            for (int i = 0; i < randomAmountOfGold; i++)
            {
                Instantiate(goldCoin, transform.position, Quaternion.identity);
            }
        }

        // Logic cho rơi Boom với tỉ lệ riêng
        int boomRoll = Random.Range(1, 101);
        if (boomRoll <= boomDropChance && boomPickup != null)
        {
            Instantiate(boomPickup, transform.position, Quaternion.identity);
        }

        // Logic cho rơi Gem với tỉ lệ riêng
        int gemRoll = Random.Range(1, 101);
        if (gemRoll <= gemDropChance && gemPickup != null)
        {
            Instantiate(gemPickup, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// Boss death drops: guaranteed large gold reward, doubled gem/boom chance,
    /// and always includes a health globe.
    /// </summary>
    public void DropBossItems()
    {
        // Always drop health globe after boss
        if (healthGlobe != null)
            Instantiate(healthGlobe, transform.position, Quaternion.identity);

        // Guaranteed 5-10 gold coins (much more than normal enemies)
        if (goldCoin != null)
        {
            int goldAmount = Random.Range(5, 11);
            for (int i = 0; i < goldAmount; i++)
                Instantiate(goldCoin, transform.position, Quaternion.identity);
        }

        // Double boom drop chance
        int boomRoll = Random.Range(1, 101);
        if (boomRoll <= Mathf.Min(boomDropChance * 2, 100) && boomPickup != null)
            Instantiate(boomPickup, transform.position, Quaternion.identity);

        // Double gem drop chance
        int gemRoll = Random.Range(1, 101);
        if (gemRoll <= Mathf.Min(gemDropChance * 2, 100) && gemPickup != null)
            Instantiate(gemPickup, transform.position, Quaternion.identity);
    }
}
