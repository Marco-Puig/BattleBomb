using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DisplayOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public GameObject Image;
    private bool mouse_over = false;

    
void Update()
    {
        Image.SetActive(false);

        if (mouse_over)
        {
            Image.SetActive(true);
            //Debug.Log("Mouse Over");
        }
    }
 
public void OnPointerEnter(PointerEventData eventData)
     {
        mouse_over = true;
        //Debug.Log("Mouse enter");
     }
 
public void OnPointerExit(PointerEventData eventData)
    {
        mouse_over = false;
        //Debug.Log("Mouse exit");
    }
}