using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryMenu : MonoBehaviour
{
    public Canvas Menu;

    void Update()
    {
    
    if (Input.GetKeyDown(KeyCode.I) || Input.GetButtonDown("Inventory"))
    {
        if(Menu.enabled == true)
            Menu.enabled = false;
        else
            Menu.enabled = true;
    }

  }
}

