using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadLevel : MonoBehaviour
{
    
    // Update is called once per frame
   public void LoadLevel1()
    {
        Application.LoadLevel("selection");
    }
    public void LoadMenu()
    {
        Application.LoadLevel("Menu");
    }
}
