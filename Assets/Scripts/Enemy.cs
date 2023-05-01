using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 100;
    int currentHealth;
    public Animator animator;
    public GameObject poof;
    public GameObject apperance;

    void Start()
    {
        currentHealth = maxHealth;
    }

 
    public void TakenDamage(int damage)
    {
        currentHealth -= damage;
        StartCoroutine(DamageAnimation());

        animator.SetTrigger("Hurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        Instantiate(poof, transform.position, Quaternion.identity);
        StartCoroutine(DestroyEnemy());
        animator.SetBool("IsDead", true);
        Debug.Log("Enemy Killed!");
        GetComponent<Collider2D>().enabled = false;
    }

        IEnumerator DestroyEnemy()
        {
          yield return new WaitForSeconds(0.1f); //change wait time depending on the length of death anim
          Destroy(this.gameObject);
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
