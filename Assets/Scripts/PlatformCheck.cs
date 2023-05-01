using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformCheck : MonoBehaviour
{

    void Start()
    {
       if (Application.platform == RuntimePlatform.WindowsPlayer)
            Debug.Log("Running on native Windows device");
        
        else if (Application.platform == RuntimePlatform.Android)
            Debug.Log("Running on native Android device");
        
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            Debug.Log("Running on native Apple device");

        else if (Application.platform == RuntimePlatform.XboxOne)
            Debug.Log("Running on Microsoft's Xbox One/S/X");

        else if (Application.platform == RuntimePlatform.Switch)
            Debug.Log("Running on native Nintendo Switch hardware");
    }
}
