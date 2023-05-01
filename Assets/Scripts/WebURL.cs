using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebURL : MonoBehaviour
{

    public void OpenDiscord()
    {
        Application.OpenURL("https://discord.com/invite/FQCSKpndKG");
        Debug.Log("Discord Opened");
    }
    
    public void OpenTwitter()
    {
        Application.OpenURL("https://twitter.com/unity3d");
        Debug.Log("Twitter Opened");
    }
    
}
