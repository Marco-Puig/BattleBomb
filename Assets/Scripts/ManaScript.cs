using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaScript : MonoBehaviour
{

    public ManaBar manaBar;

    [Range(0, 100)] public int totalMana = 100;
	[Min(0)] public int currentMana;

    public IEnumerator gettingMana()
    {
		while(true)
		{
		if (currentMana < 100)
        {	
			currentMana += 1 ;
			manaBar.SetHealth(currentMana);
			yield return new WaitForSeconds(5);
		}
		else
		{
		yield return null;
		}
    }
  }
}
