using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityStandardAssets.CrossPlatformInput;

public class Player_Main : PlayerControllerMain
{
    public Transform attackPoint;
    public LayerMask enemyLayers;

    public PrefabWeapon prefabWeapon;
    public ManaBar manaBar;

    public ManaScript ManaScript;
    public ComboSystem comboSystem;

    public float range = 0.5f;
    public int attackDamage = 20;

    public float attackRate = 2f;
    float nextAttacktime = 0f;

    private void Start()
    {
        m_CapsulleCollider = this.transform.GetComponent<CapsuleCollider2D>();
        m_Anim = this.transform.Find("model").GetComponent<Animator>();
        m_rigidbody = this.transform.GetComponent<Rigidbody2D>();     
        
    }

    private void Update()
    {
        m_MoveX = Input.GetAxis("Horizontal");

        checkInput();

        if (m_rigidbody.velocity.magnitude > 30)
        {
            m_rigidbody.velocity = new Vector2(m_rigidbody.velocity.x - 0.1f, m_rigidbody.velocity.y - 0.1f);

        }
    }

    void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            ManaScript.currentMana += 10;
            manaBar.slider.value += 10;
            nextAttacktime = Time.time + 1f / attackRate;
            enemy.GetComponent<Enemy>().TakenDamage(attackDamage);
        }
    }
    void MegaAttack()
    {
        ManaScript.currentMana -= 20;
        manaBar.slider.value -= 20;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            nextAttacktime = Time.time + 1f / attackRate;
            enemy.GetComponent<Enemy>().TakenDamage(attackDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.DrawWireSphere(attackPoint.position, range);
    } 

    public void checkInput()
    {


        if (Input.GetKeyDown(KeyCode.S)) 
        {

           // IsSit = true;
           // m_Anim.Play("Sit");


        }
        else if (Input.GetKeyUp(KeyCode.S))
        {

           // m_Anim.Play("Idle");
           // IsSit = false;

        }


        if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Sit") || m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Die"))
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (currentJumpCount < JumpCount)  
                {
                    DownJump();
                }
            }

            return;
        }

   
        GroundCheckUpdate();


        if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
          {
            if (Input.GetKey(KeyCode.Mouse0) || Input.GetButton("Fire1"))
            {
                if (Input.GetKey(KeyCode.D) && isGrounded || (m_MoveX >= 0.1 && isGrounded))
                {
                Attack();
                m_Anim.Play("Run Slashing");
                }
                else if (Input.GetKey(KeyCode.A) && isGrounded || (m_MoveX <= -0.1 && isGrounded))
                {
                Attack();
                m_Anim.Play("Run Slashing");
                }
                else
                {
                comboSystem.Combo();
                }
            }
            
        

        else if (Input.GetKey(KeyCode.Mouse1) && ManaScript.currentMana > 20 || Input.GetButton("Fire2") && ManaScript.currentMana > 20)
        {
            MegaAttack();
            m_Anim.Play("MegaSlashing");
        }

     // else if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing")) //seperate script called abilities and make prefab weapon "f" key
         // {
         //   if (Input.GetKey(KeyCode.Mouse1))
          // {
           //   Attack();
          //    m_Anim.Play("MegaSlashing");
          //  }
         // }
            else
            {
                comboSystem.comboStep = 0;

                if (m_MoveX == 0)
                {
                    if (!OnceJumpRayCheck)
                        m_Anim.Play("Idle");

                }
                else
                {

                    m_Anim.Play("Walking");
                }

            }
        }



        if (Input.GetKey(KeyCode.D) || (m_MoveX >= 0.1))
        {

            if (isGrounded)  
            {


            //  if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
            //      return;

                transform.transform.Translate(Vector2.right* m_MoveX * MoveSpeed * Time.deltaTime);




            }
            else
            {

                transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));

            }




           if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
                return;

            if (!Input.GetKey(KeyCode.A))
                Flip(false);


        }
        else if (Input.GetKey(KeyCode.A) || (m_MoveX <= -0.1))
        {

            if (isGrounded)  
            {



              //  if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
              //     return;


                transform.transform.Translate(Vector2.right * m_MoveX * MoveSpeed * Time.deltaTime);

            }
            else
            {

                transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));

            }


           if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
                return;

            if (!Input.GetKey(KeyCode.D))
                Flip(true);

        }


    if (Input.GetKeyDown(KeyCode.Space) || Input.GetButton("Jump"))
    {
        if (currentJumpCount < JumpCount)  
        {

            if (!IsSit)
            {
                performJump();
                m_Anim.Play("Jump Loop");

            }
            else
            {
                DownJump();
                m_Anim.Play("Jump Start");

            }

        }


    }

}


   protected override void LandingEvent()
   {


    if (!m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Running") && !m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
        m_Anim.Play("Idle");

   }


}