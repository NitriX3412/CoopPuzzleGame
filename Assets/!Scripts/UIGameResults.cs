using FishNet;
using FishNet.Connection;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIGameResults : MonoBehaviour
{
    [SerializeField] private Canvas resultsPanel;
    [SerializeField] private TextMeshProUGUI resultsText;

    private void Start()
    {
        if (resultsPanel == null)
        {
            Debug.LogWarning("No Result Panel Assignet");
        }
    }

    public void ShowScore(GameManager.PlayerScoreData[] results)
    {
        resultsText.text = "";
        foreach (var entry in results)
        {
            resultsText.text += $"{entry.Nickname} score: {entry.Score}\n";
        }

        resultsPanel.gameObject.SetActive(true);
    }

    public void HideScore()
    {
        resultsPanel.gameObject.SetActive(false);
    }

    public void ExitGame()
    {
        if (InstanceFinder.ServerManager != null && InstanceFinder.ServerManager.Started)
        {
            var clients = InstanceFinder.ServerManager.Clients;
            foreach (NetworkConnection conn in clients.Values)
            {
                conn.Disconnect(true);
            }
        }

        else if (InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started)
        {
            InstanceFinder.ClientManager.StopConnection();
        }

        Application.Quit();
    }
}
