using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject loadGamePanel;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        loadGamePanel.SetActive(false);
    }

    public void OpenLoadGame()
    {
        mainMenuPanel.SetActive(false);
        loadGamePanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        loadGamePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
