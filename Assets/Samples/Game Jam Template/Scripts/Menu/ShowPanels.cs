using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class ShowPanels : MonoBehaviour {

	public GameObject optionsPanel;							//Store a reference to the Game Object OptionsPanel 
	public GameObject optionsTint;							//Store a reference to the Game Object OptionsTint 
	public GameObject menuPanel;							//Store a reference to the Game Object MenuPanel 
	public GameObject pausePanel;                           //Store a reference to the Game Object PausePanel 
	public GameObject shopPanel;
	public GameObject quitPanel;
	public GameObject dailyPanel;
	public GameObject scorePanel;

	private GameObject activePanel;                         
    private MenuObject activePanelMenuObject;
    private EventSystem eventSystem;



    private void SetSelection(GameObject panelToSetSelected)
    {

        activePanel = panelToSetSelected;
        activePanelMenuObject = activePanel.GetComponent<MenuObject>();
        if (activePanelMenuObject != null)
        {
            activePanelMenuObject.SetFirstSelected();
        }
    }

    public void Start()
    {
        SetSelection(menuPanel);
    }

    //Call this function to activate and display the Options panel during the main menu
    public void ShowOptionsPanel()
	{
		optionsPanel.SetActive(true);
		optionsTint.SetActive(true);
		quitPanel.SetActive(false);
		menuPanel.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
		scorePanel.SetActive(false);
		SetSelection(optionsPanel);

    }
	
	public void ShowScorePanel()
	{
		scorePanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(true);
		quitPanel.SetActive(false);
		menuPanel.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);

    }

	public void HideOptionsPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}
	public void ShowDailyPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(true);
		menuPanel.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(true);
	}
	
	public void HideScorePanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}

	public void HideDailyPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}
	public void ShowShopPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(true);
		menuPanel.SetActive(false);
		shopPanel.SetActive(true);
		dailyPanel.SetActive(false);
	}
	public void HideShopPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}
	public void ShowQuitPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(true);
		menuPanel.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);

	}
	public void HideQuitPanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive(true);
		optionsPanel.SetActive(false);
		optionsTint.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}


	//Call this function to activate and display the main menu panel during the main menu
	public void ShowMenu()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive (true);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
		SetSelection(menuPanel);
    }

	//Call this function to deactivate and hide the main menu panel during the main menu
	public void HideMenu()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		menuPanel.SetActive (false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}
	
	//Call this function to activate and display the Pause panel during game play
	public void ShowPausePanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		pausePanel.SetActive (true);
		optionsTint.SetActive(true);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
		SetSelection(pausePanel);
    }

	//Call this function to deactivate and hide the Pause panel during game play
	public void HidePausePanel()
	{
		scorePanel.SetActive(false);
		quitPanel.SetActive(false);
		pausePanel.SetActive (false);
		optionsTint.SetActive(false);
		shopPanel.SetActive(false);
		dailyPanel.SetActive(false);
	}
}
