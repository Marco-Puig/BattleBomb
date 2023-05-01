using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponsMenu : MonoBehaviour
{
    public GameObject fireSword;
    public GameObject oldSword;
    public GameObject crystalSword;
    public GameObject blueSword;
    public GameObject katana;
    public GameObject pinkSword;
    public GameObject pirateSword;
    public GameObject bigRedSword;
    public GameObject magicalstaff;
    public GameObject saberSword;
    public GameObject greenSword;
    public GameObject bSword;
    public GameObject fireIcon;
    public GameObject oldIcon;
    public GameObject crystalIcon;
    public GameObject blueIcon;
    public GameObject katanaIcon;
    public GameObject pinkIcon;
    public GameObject pirateIcon;
    public GameObject bigRedIcon;
    public GameObject magicalStaffIcon;
    public GameObject saberSwordIcon;
    public GameObject greenSwordIcon;
    public GameObject bSwordIcon;
    public GameObject menu;

    public GameObject[] weapons; 

    
    public Player_Main movement;
    public Animator animator;

    public void Update() 
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            animator.enabled = false;
            menu.SetActive(true);
            movement.enabled = false;
        }
    }
    public void confirmClick()
    {
        animator.enabled = true;
        menu.SetActive(false);
        movement.enabled = true;
    }
    public void staffClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(true);
        magicalStaffIcon.SetActive(true);
    }
    public void fireSwordClick()
    {
        //weapons[].SetActive(false);
        //weapons.fireSword.SetActive(true);
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(true);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(true);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void bigRedSwordClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(true);
        bigRedSword.SetActive(true);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }

    public void oldSwordClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(true);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(true);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void crystalSwordClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(true);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(true);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void blueSwordClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(true);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(true);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void katanaClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(true);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(true);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void pinkClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(true);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(true);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void pirateClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(true);
        pirateSword.SetActive(true);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void saberClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(true);
        saberSwordIcon.SetActive(true);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
    public void greenClick()
    {
        bSword.SetActive(false);
        bSwordIcon.SetActive(false);
        greenSword.SetActive(true);
        greenSwordIcon.SetActive(true);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }
        public void bClick()
    {
        bSword.SetActive(true);
        bSwordIcon.SetActive(true);
        greenSword.SetActive(false);
        greenSwordIcon.SetActive(false);
        saberSword.SetActive(false);
        saberSwordIcon.SetActive(false);
        fireSword.SetActive(false);
        oldSword.SetActive(false);
        crystalSword.SetActive(false);
        blueSword.SetActive(false);
        katana.SetActive(false);
        pinkSword.SetActive(false);
        fireIcon.SetActive(false);
        oldIcon.SetActive(false);
        crystalIcon.SetActive(false);
        blueIcon.SetActive(false);
        katanaIcon.SetActive(false);
        pinkIcon.SetActive(false);
        pirateIcon.SetActive(false);
        pirateSword.SetActive(false);
        bigRedIcon.SetActive(false);
        bigRedSword.SetActive(false);
        magicalstaff.SetActive(false);
        magicalStaffIcon.SetActive(false);
    }

}
