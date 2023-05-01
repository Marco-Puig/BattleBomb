using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectionOld : MonoBehaviour
{
    private int selectedCharacterIndex;
    private Color desiredColor;
    public List<CharacterSelectObject> characterList = new List<CharacterSelectObject>();
    public TextMeshProUGUI characterName;
    public Image characterSplash;
    public Image backgroundColor;

    public GameObject firePlayer;
    public GameObject waterPlayer;
    public GameObject electricPlayer;
    public GameObject normalPlayer;

    public Transform respawnTransform;

    private void Start()
    {       
        UpdateCharacterSelectionUI();      
    }

    public void LeftArrow() 
    {
        selectedCharacterIndex++;
        if (selectedCharacterIndex == characterList.Count);
        selectedCharacterIndex = 0;

        UpdateCharacterSelectionUI();
    }
    
    public void RightArrow()
    {
        selectedCharacterIndex--;
        if (selectedCharacterIndex < 0)
            selectedCharacterIndex = characterList.Count - 1;
        UpdateCharacterSelectionUI();
    }

    public void Confirm()
    {
        Debug.Log(string.Format("character select"));
        SceneManager.LoadScene("TrainingGrounds");

        if (selectedCharacterIndex == 0)
        {
            StartCoroutine(SpawnRed());
        }
        else if (selectedCharacterIndex == 3)
        {
            StartCoroutine(SpawnNormal());
        }

    }
    IEnumerator SpawnRed()
    {
        yield return new WaitForSeconds (2);
        Instantiate(firePlayer, respawnTransform.position, Quaternion.identity);
    }

    IEnumerator SpawnNormal()
    {
        yield return new WaitForSeconds(2);
        Instantiate(normalPlayer, respawnTransform.position, Quaternion.identity);
    }

    private void UpdateCharacterSelectionUI()
    {
        characterSplash.sprite = characterList[selectedCharacterIndex].splash;
        characterName.text = characterList[selectedCharacterIndex].characterName;
        desiredColor = characterList[selectedCharacterIndex].characterColor;
    }

    [System.Serializable] 

    public class CharacterSelectObject
    {
        public Sprite splash;
        public string characterName;
        public Color characterColor;
    }
}