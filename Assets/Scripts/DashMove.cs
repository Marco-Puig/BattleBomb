using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashMove : PlayerControllerMain
{
    private Rigidbody2D rb;
    public Collider2D hitBox;
    public float dashSpeed;
    private float dashTime;
    public float startDashTime;
    private int direction;
    public GameObject dashEffect;

    public ManaScript Mana;

    public int count = 0;

    public float doubleTapTime = 0.5f;
    public float dashWaitTime = 2.0f;
    private float _lastDashButtonTime;
    private float _lastDashTime;

    private float moveX = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hitBox = GetComponent<Collider2D>();
        dashTime = startDashTime;
		Mana.currentMana = Mana.totalMana;
		Mana.manaBar.SetMaxHealth(Mana.totalMana);

    }

    void Update()
    {   	
        moveX = Input.GetAxis("Horizontal");

        if (Mana.currentMana > Mana.totalMana)
        {
			Mana.currentMana = Mana.totalMana;
        }

        if (direction == 0)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKey(KeyCode.A) && isDashPossibleA() || Input.GetButton("DashL") && moveX <= -0.1 && isDashPossibleA())
            {
            if (Time.time - _lastDashButtonTime < doubleTapTime)
            Flip(true);
            direction = 1;
            dashEffect.SetActive(true);
            StartCoroutine(EffectBackOff());
            // TakeLoss(10);
            //hitBox.enabled = false;
            _lastDashButtonTime = Time.time;
         }

            if (Input.GetKeyDown(KeyCode.LeftShift) && Input.GetKey(KeyCode.D) && isDashPossibleA() || Input.GetButton("DashR") && moveX >= 0.1 && isDashPossibleA()) 
            {
            if (Time.time - _lastDashButtonTime < doubleTapTime)
            Flip(false);
            direction = 2;
            dashEffect.SetActive(true);
            StartCoroutine(EffectBackOff());
            // TakeLoss(10);
            //hitBox.enabled = false;
            _lastDashButtonTime = Time.time;
         }
        }
        
        else
      {
         if (dashTime <= 0)
        {
            direction = 0;
            dashTime = startDashTime;
            rb.velocity = Vector2.zero;
        }
        
        else
        {
            dashTime -= Time.deltaTime;
            
            if (direction == 1)
            {
                rb.velocity = Vector2.left * dashSpeed;
            }
            else if (direction == 2)
            {
                rb.velocity = Vector2.right * dashSpeed;
            }
        }
    }
         bool isDashPossibleA() 
         {
             return Time.time - _lastDashButtonTime > dashWaitTime;
         }

          bool isDashPossibleD() 
         {
             return Time.time - _lastDashButtonTime > dashWaitTime;
         }

    }

    void FixedUpdate()
	{
		if (Mana.currentMana <= 99)
		{
			StartCoroutine(gettingMana());
		}
		if (Mana.currentMana > Mana.totalMana)
        {
			Mana.currentMana = Mana.totalMana;
        }
	}

    void TakeLoss(int drain)
	{
		Mana.currentMana -= drain;
		Mana.manaBar.SetHealth(Mana.currentMana);
    }
    void ReturnMana(int regen)
	{
		Mana.currentMana += regen;
		Mana.manaBar.SetHealth(Mana.currentMana);
	}

    IEnumerator gettingMana()
    {

		while (Mana.currentMana<100)
        {
			yield return new WaitForSeconds(1);
			count++;
			if(count%159 == 0)
			{
				Mana.currentMana++;
				Mana.manaBar.SetHealth(Mana.currentMana);
				count = 0;
			}
        }
    }
      protected override void LandingEvent()
   {

   //here

   }
       IEnumerator EffectBackOff()
    {

        yield return new WaitForSeconds(0.1f);
        dashEffect.SetActive(false);
        hitBox.enabled = true;
    }
}  