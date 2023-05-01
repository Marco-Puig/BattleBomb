using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifetime : MonoBehaviour
{
    public float TimeToLive = 5f;
   
    public void Start()
    {
        Destroy(gameObject, TimeToLive);
    }
}