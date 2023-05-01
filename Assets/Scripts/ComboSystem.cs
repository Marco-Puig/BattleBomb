using UnityEngine;
using System.Collections.Generic;
using System.Collections;
 
public class ComboSystem : MonoBehaviour {
 
public Animator playerAnim;
public bool comboPossible;
public int comboStep;


    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetButton("Fire1"))
        {
            Attack();
        }
        else
        {
            comboStep = 0;
        }
    }
    
    public void Attack()
    { 
        if(comboStep == 0) 
        {
            playerAnim.Play("Slashing");
            comboStep = 1;
            return;
        }
        if(comboStep != 0)
        {
            if(comboPossible)
                {
                    comboPossible = false;
                    comboStep += 1;
                }
        }
    }

    public void ComboPossible()
    {
        comboPossible = true;
    }
    public void Combo()
    {
        if(comboStep == 2)
        playerAnim.Play("Slashing 2");
        if(comboStep == 3)
        playerAnim.Play("Slashing 3");
    }
    public void ComboReset()
        {
            comboPossible = false;
            comboStep = 0;
        }
}