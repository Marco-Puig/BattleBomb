using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsoleMenu : MonoBehaviour
{
    public GameObject Menu;
    void Update()
    {
    
    if (Input.GetKeyDown(KeyCode.F1) || Input.GetButtonDown("Console"))
        Menu.SetActive(!Menu.activeSelf);
    
    }
}
