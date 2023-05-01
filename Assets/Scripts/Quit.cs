using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quit : MonoBehaviour
{
    public void LoadScene(int level)
    {
        Application.LoadLevel(level);
        Destroy(this);
    }
}