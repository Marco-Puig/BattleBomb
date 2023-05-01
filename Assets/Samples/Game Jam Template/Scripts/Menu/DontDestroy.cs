using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroy : MonoBehaviour {

   	private GameObject Player;
    private GameObject Camera;

    public void Awake()
    {
        GameObject Player = GameObject.FindGameObjectWithTag("Player");
        GameObject Camera = GameObject.FindGameObjectWithTag("MainCamera");
        DontDestroyOnLoad(Player);
        DontDestroyOnLoad(Camera);
    }
}
