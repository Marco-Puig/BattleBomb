using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUIDisplay : MonoBehaviour
{
    public GameObject text;

    public void OnTriggerStay2D(Collider2D other)
    {
        text.SetActive(true);
        Debug.Log("item picked up");
        var item = other.GetComponent<GroundItem>();
        if (item)
        {
            text.SetActive(false);
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {

        text.SetActive(false);
    }


}
