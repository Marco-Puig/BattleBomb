using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyIfDetected : MonoBehaviour
{
    private GameObject Camera;
    private GameObject Player;

    void Update()
    {
        GameObject Camera = GameObject.FindGameObjectWithTag("MainCamera");
        Destroy(Camera);
    } 
    void FixedUpdate()
    {
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        Destroy(Player);
    }
}
