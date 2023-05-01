using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthItem : MonoBehaviour {

    private Health player;
    private HealthBar playerHud;
    public GameObject healthEffect;
    public int playerHealth;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Health>();
        playerHud = GameObject.FindGameObjectWithTag("UI").GetComponent<HealthBar>();
        playerHealth = player.currentHealth;
    }

    public void Use() {
        Instantiate(healthEffect, player.transform.position, Quaternion.identity);
        playerHealth += 20;
        player.currentHealth += 20;
        playerHud.SetHealth(playerHealth);
        Destroy(gameObject);
    }
}
