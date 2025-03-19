using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ResetUI : NetworkBehaviour
{
    [SerializeField] private Button resetButton;



    private void Awake()
    {
        resetButton.onClick.AddListener(() => {
            var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
            var playerNetworkScript = playerObject.GetComponent<PlayerNetwork>();
            playerNetworkScript.ResetPlayer(); 
            ResetScene();
        });
    }

    private void ResetScene()
    {
        GameObject winScreen = GameObject.Find("Canvas/EndScreen/GameOverWin");
        GameObject loseScreen = GameObject.Find("Canvas/EndScreen/GameOverLose");

        winScreen.SetActive(false);
        loseScreen.SetActive(false);
    }
}
