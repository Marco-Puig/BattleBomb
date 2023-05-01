using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisplayNameOnLoad : MonoBehaviour
{
    private GameObject DisplayText;


    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject DisplayText = GameObject.FindGameObjectWithTag("TitleText");
        //activate text
        StartCoroutine(ExampleCoroutine());
    }

    IEnumerator ExampleCoroutine()
    {

        yield return new WaitForSeconds(5);
        //text goes away

    }
}