using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetFollow : MonoBehaviour
{
    public float speed;
    public float stoppingDistance;

    private Transform target;

    void Update()
    {
        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        //maybe an issue when working on multiplayer, or may keep as a feature that pets can assist other players.

        if (Vector2.Distance(transform.position, target.position) > stoppingDistance)
        {
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
            //How it Works: transform.position = Vector2.MoveTowards(from, to, speed); 
                   
            if (Input.GetKeyDown(KeyCode.D))
            {
                //flip pet gameObject
                transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                //flip pet gameObject
                transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }

        
    }
}
