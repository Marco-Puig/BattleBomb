using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionChange : MonoBehaviour
{
    public float smooth = 1f;

    private Vector3 targetAngles;

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.D))
        {
            targetAngles = transform.eulerAngles = 0f * Vector3.up;

            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, targetAngles, smooth * Time.deltaTime);

        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            targetAngles = transform.eulerAngles =-180f * Vector3.up;

            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, targetAngles, smooth * Time.deltaTime);
        }
    }
}