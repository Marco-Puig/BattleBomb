using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{

	public int maxHealth = 100;
	public int currentHealth;

	public HealthBar healthBar;
	public GameObject deathEffect;

	public GameObject characterModel;
	public GameObject interfaceMenu;
	public GameObject gameOverScreen;
	public Animator animator;
	public Player_Main swordMan;

	private GameObject camera;
	//private GameObject player;
	private GameObject audio;

	void Start()
	{
		currentHealth = maxHealth;
		healthBar.SetMaxHealth(maxHealth);
	}

	void Update()
	{
		camera = GameObject.FindGameObjectWithTag("MainCamera");
		//player = GameObject.FindGameObjectWithTag("Player");
		audio = GameObject.FindGameObjectWithTag("Music");

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			TakenDamage(10);
		}
	}

	public void TakenDamage(int damage)
	{
		currentHealth -= damage;
		healthBar.SetHealth(currentHealth);
		StartCoroutine(DamageAnimation());

		if (currentHealth == 0)
		{
			//player can only take damage in even numbers.
			swordMan.enabled = false;
			StartCoroutine(Dead());
			animator.SetBool("PlayerDeathAnim", true);
			Destroy(interfaceMenu);
		}
	}

	IEnumerator Dead()
	{
		yield return new WaitForSeconds(1);
		Instantiate(deathEffect, transform.position, Quaternion.identity);
		gameOverScreen.SetActive(true);
		audio.SetActive(false);
		yield return new WaitForSeconds(1);
		characterModel.SetActive(false);
		yield return new WaitForSeconds(10);
		gameOverScreen.SetActive(false);
		SceneManager.LoadScene("TrainingGrounds"); //temp
		Destroy(GetComponent<Camera>());
		audio.SetActive(true);
	}

	IEnumerator DamageAnimation()
	{
		SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();

		for (int i = 0; i < 3; i++)
		{
			foreach (SpriteRenderer sr in srs)
			{
				Color c = sr.color;
				c.a = 0;
				sr.color = c;
			}

			yield return new WaitForSeconds(.1f);

			foreach (SpriteRenderer sr in srs)
			{
				Color c = sr.color;
				c.a = 1;
				sr.color = c;
			}

			yield return new WaitForSeconds(.1f);
		}
	}
}