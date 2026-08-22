using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player_MainMobile : PlayerControllerMain
{
   // public float DashForce;
   // public float StartDashTime;
   // float CurrentDashTime;
   // bool isDashing;
   // float DashDirection;
   // float m_MoveX;

    public Transform attackPoint;
    public LayerMask enemyLayers;

    public PrefabWeapon prefabWeapon;
    public ManaBar manaBar;

    public ManaScript ManaScript;
    public ComboSystem comboSystem;

    public Joystick Joystick;

    private bool attackRight;
    private bool attackLeft;
    private bool JumpButton;
    public bool dash;
    public bool Interact;

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
        m_MoveX = Joystick.Horizontal;

        checkInput();

        if (m_rigidbody.velocity.magnitude > 30)
        {
            m_rigidbody.velocity = new Vector2(m_rigidbody.velocity.x - 0.1f, m_rigidbody.velocity.y - 0.1f);

        }
    }

   public void PointerDownRight()
    {
        attackRight = true;
    }

    public void PointerUpRight()
    {
        attackRight = false;
    }
    
    public void PointerDownLeft()
    {
        attackLeft = true;
    }

    public void PointerUpLeft()
    {
        attackLeft = false;
    }

    public void PointerDownJump()
    {
        JumpButton = true;
    }

    public void PointerUpJump()
    {
        JumpButton = false;
    }

    public void PointerDownInteract()
    {
        Interact = true;
    }

    public void PointerUpInteract()
    {
        Interact = false;
    }

    public void PointerDownDash()
    {
        dash = true;
    }

    public void PointerUpDash()
    {
        dash = false;
    }
 


    void Attack()
    {
        //detect
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, range, enemyLayers);
        //damage
        foreach (Collider2D enemy in hitEnemies)
        {
            ManaScript.currentMana += 20;
            manaBar.slider.value += 20;
            Debug.Log(enemy.name + "has taken damage");
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
            if (JumpButton)
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
            if (attackLeft)
            {
                if (Joystick.Horizontal >= .2f && isGrounded)
                {
                Attack();
                m_Anim.Play("Run Slashing");
                }
                else if (Joystick.Horizontal <= -.2f && isGrounded)
                {
                Attack();
                m_Anim.Play("Run Slashing");
                }
                else
                {
                Attack();
                comboSystem.Attack();
                }
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

                if (Joystick.Horizontal == 0)
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



        if (Joystick.Horizontal >= .2f)
        {

            if (isGrounded)  
            {


            //  if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            //      return;

                transform.transform.Translate(Vector2.right* m_MoveX * MoveSpeed * Time.deltaTime);




            }
            else
            {

                transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));

            }




           if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
                return;

            if (Joystick.Horizontal >= -.2f)
                Flip(false);


        }
        else if (Joystick.Horizontal <= -.2f)
        {

            if (isGrounded)  
            {



              //  if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
              //     return;


                transform.transform.Translate(Vector2.right * m_MoveX * MoveSpeed * Time.deltaTime);

            }
            else
            {

                transform.transform.Translate(new Vector3(m_MoveX * MoveSpeed * Time.deltaTime, 0, 0));

            }


           if (m_Anim.GetCurrentAnimatorStateInfo(0).IsName("Slashing"))
                return;

            if (Joystick.Horizontal <= .2f)
                Flip(true);


        }


    if (JumpButton)
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