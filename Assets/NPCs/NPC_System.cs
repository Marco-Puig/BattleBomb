using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

 
public class NPC_System : MonoBehaviour {

public float dialogueTime;
public GameObject text;
public GameObject dialogue;
Player_MainMobile mobileScript;


    void OnTriggerStay2D(Collider2D other)
    {  
        mobileScript = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_MainMobile>();

            if (Input.GetKey(KeyCode.W) || Input.GetButton("Interact"))
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    dialogue.SetActive(true);
                    text.SetActive(false);
                    StartCoroutine(ListenToNPC());
                }
            }
            if (mobileScript.Interact)
                {
                if (other.gameObject.CompareTag("Player"))
                {
                    dialogue.SetActive(true);
                    text.SetActive(false);
                    StartCoroutine(ListenToNPC());
                }
            }
        }
    
    IEnumerator ListenToNPC()
    {
        yield return new WaitForSeconds(dialogueTime);
        text.SetActive(true);
        dialogue.SetActive(false);
    }
}