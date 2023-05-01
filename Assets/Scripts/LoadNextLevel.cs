using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

 
public class LoadNextLevel : MonoBehaviour {

private GameObject camera;
public GameObject text;
Player_MainMobile mobileScript;


    void OnTriggerStay2D(Collider2D other)
    {  
        camera = GameObject.FindGameObjectWithTag("MainCamera");
        mobileScript = GameObject.FindGameObjectWithTag("Player").GetComponent<Player_MainMobile>();
        text.SetActive(true);

            if (Input.GetKey(KeyCode.W) || Input.GetButton("Interact"))
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                    DontDestroyOnLoad(other);
                    DontDestroyOnLoad(camera);
                }
            }
            if (mobileScript.Interact)
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
                    DontDestroyOnLoad(other);
                    DontDestroyOnLoad(camera);
                }
            }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        text.SetActive(false);
    }
}