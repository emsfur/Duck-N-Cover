using UnityEngine;
using Unity.Netcode;
using TMPro;

public class TankHealthManager : NetworkBehaviour
{
    private TextMeshProUGUI p1HealthUI;
    private TextMeshProUGUI p2HealthUI;

    [SerializeField] private AudioSource loseSound;
    [SerializeField] private AudioSource winSound;

    private NetworkVariable<int> p1Health = new NetworkVariable<int>(
        100, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> p2Health = new NetworkVariable<int>(
        100, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private GameObject winScreen;
    private GameObject loseScreen;

    public override void OnNetworkSpawn()
    {
        p1HealthUI = GameObject.Find("Canvas/TankHealthUI/P1Health").GetComponent<TextMeshProUGUI>();
        p2HealthUI = GameObject.Find("Canvas/TankHealthUI/P2Health").GetComponent<TextMeshProUGUI>();

        p1HealthUI.text = "P1 Health: " + p1Health.Value;
        p2HealthUI.text = "P2 Health: " + p2Health.Value;

        // Update UI when health changes
        p1Health.OnValueChanged += (oldValue, newValue) => {    p1HealthUI.text = "P1 Health: " + newValue;    };
        p2Health.OnValueChanged += (oldValue, newValue) => {    p2HealthUI.text = "P2 Health: " + newValue;    };


        winScreen = GameObject.Find("Canvas/EndScreen/GameOverWin");
        loseScreen = GameObject.Find("Canvas/EndScreen/GameOverLose");
    }


    public void ResetHealth(string tank)
    {
        if (IsServer)
        {
            SetHealthServerRpc(tank, 100);
            
        }
    }

    public void missileHit(string tank)
    {
        if (IsServer)
        {
            int damage = Random.Range(15, 25);

            SetHealthServerRpc(tank, p1Health.Value - damage, p2Health.Value - damage);
            
        }
    }

    [ServerRpc]
    private void SetHealthServerRpc(string tank, int health1 = 100, int health2 = 100)
    {
        if (tank == "P1Tank(Clone)")
        {
            p1Health.Value = health1;
        }
        else if (tank == "P2Tank(Clone)")
        {
            p2Health.Value = health2;
        }

        if (p1Health.Value <= 0) {
            if (OwnerClientId == 0)
            {
                loseScreen.SetActive(true);
                loseSound.Play();
                    
            } else if (OwnerClientId == 1)
            {
                winScreen.SetActive(true);
                winSound.Play();
            }
        }
        else if (p2Health.Value <= 0)
        {
            if (OwnerClientId == 0)
            {
                winScreen.SetActive(true);
                winSound.Play();
            }
            else if (OwnerClientId == 1)
            {
                loseScreen.SetActive(true);
                loseSound.Play();
            }
                
        }
    }

}
