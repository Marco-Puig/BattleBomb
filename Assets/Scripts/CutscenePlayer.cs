using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

 
public class CutscenePlayer: MonoBehaviour {

public float playTime = 17;
    
    void Start()
    {
        StartCoroutine(Fade());
    }
    
    IEnumerator Fade() 
    {         
        Debug.Log("Playing Cutscene");
        yield return new WaitForSeconds(playTime); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Debug.Log("Cutscene is done");
    }
}
