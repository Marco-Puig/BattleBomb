using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabWeapon : MonoBehaviour
{

	public Transform firePoint;
	public GameObject bulletPrefab;

	public ManaScript Mana;


	void Start()
	{
		Mana.currentMana = Mana.totalMana;
		Mana.manaBar.SetMaxHealth(Mana.totalMana);
		StartCoroutine(Mana.gettingMana());
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.E) && Mana.currentMana > 20 || Input.GetButtonDown("SpellUse") && Mana.currentMana > 20)
		{
			TakeLoss(20);
		}
		if (Mana.currentMana > Mana.totalMana)
        {
			Mana.currentMana = Mana.totalMana;
        }
	}
	void FixedUpdate()
	{
		if (Mana.currentMana > Mana.totalMana)
        {
			Mana.currentMana = Mana.totalMana;
        }
	}
	void TakeLoss(int drain)
	{
		Mana.currentMana -= drain;
		Mana.manaBar.SetHealth(Mana.currentMana);

		if (Mana.currentMana > 0)
		{
			GameObject clone = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
			clone.GetComponent<Rigidbody2D>().AddForce(transform.forward * 100);
		}
	}
	void ReturnMana(int regen)
	{
		Mana.currentMana += regen;
		Mana.manaBar.SetHealth(Mana.currentMana);
	}
}
