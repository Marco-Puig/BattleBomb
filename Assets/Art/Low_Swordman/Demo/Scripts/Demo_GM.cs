using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;




public class Demo_GM : MonoBehaviour {



    public static Demo_GM Gm;

    public Image[] UIImage;

    // Use this for initialization
    void Awake () {
    
         Gm = this;
    }
	
	// Update is called once per frame
	void Update () {

        KeyUPDownchange();


    }


    void InitColor()
    {

        for (int i = 0; i < UIImage.Length; i++)
        {
            UIImage[i].color = new Color(0, 0, 0);


        }

    }

    public void KeyUPDownchange()
    {
        // wsad
        if (Input.GetKeyUp(KeyCode.A))
        {
            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[2].color = myColor;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[3].color = myColor;
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[0].color = myColor;
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[1].color = myColor;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[2].color = myColor;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[3].color = myColor;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[0].color = myColor;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[1].color = myColor;
        }

        ///
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            Color myColor = new Color32(0, 0, 0, 225);

            Demo_GM.Gm.UIImage[4].color = myColor;
        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            Color myColor = new Color32(0, 0, 0, 225);

            Demo_GM.Gm.UIImage[5].color = myColor;
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            Color myColor = new Color32(0, 0, 0, 225);

            Demo_GM.Gm.UIImage[7].color = myColor;
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {

            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[4].color = myColor;

        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {



            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[5].color = myColor;

        }

    

        if (Input.GetKeyDown(KeyCode.I))
        {

            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[6].color = myColor;
        }


    
        if (Input.GetKeyUp(KeyCode.I))
        {

            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[6].color = myColor;
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {

            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[7].color = myColor;
        }



        if (Input.GetKeyUp(KeyCode.Space))
        {

            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[7].color = myColor;
        }
        
        
        if (Input.GetKeyDown(KeyCode.E))
        {

            Color myColor = new Color32(25, 25, 25, 140);

            Demo_GM.Gm.UIImage[8].color = myColor;
        }



        if (Input.GetKeyUp(KeyCode.E))
        {

            Color myColor = new Color32(0, 0, 0, 168);

            Demo_GM.Gm.UIImage[8].color = myColor;
        }


    }

}
