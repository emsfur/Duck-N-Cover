using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;

public class PlayerNetwork : NetworkBehaviour
{
    private CinemachineVirtualCamera virtualCam;
    private Rigidbody2D rb;

    [SerializeField] private GameObject P1TankPrefab;
    [SerializeField] private GameObject P2TankPrefab;
    private GameObject playerTank;

    private Rigidbody2D tankrb;
    private GameObject duckTankOuter;

    private GameObject barrel;
    private GameObject emptyBarrel;
    private GameObject loadedBarrel;

    private GameObject meterMask;

    [SerializeField] private GameObject missilePrefab;
    private GameObject missileInst;
    private Transform missileSpawnPoint;
    private int missilePowerLevel = 9;

    // these interactable variables are only to be kept locally, never modified/checked over server
    private bool nearDriverSeat = false;
    private bool nearShooter = false;
    private bool nearAmmo = false;
    private bool inCar = false;
    private bool inShoot = false;
    private bool holdingAmmo = false;

    private int loadedAmmo = 0;

    [SerializeField] private AudioSource barrelMoveSound;
    [SerializeField] private AudioSource explosionSound;
    [SerializeField] private AudioSource missileFireSound;
    [SerializeField] private AudioSource tankMoveSound;
    [SerializeField] private AudioSource missileLoadSound;


    public override void OnNetworkSpawn()
    {
        // placed outside of the IsOwner check to allow the server to control player movement
        rb = GetComponent<Rigidbody2D>();

        if (IsOwner) {    
            SetPlayerSpawnServerRpc(OwnerClientId);
            SetupVirtualCam();
            SpawnPlayerTankServerRpc(OwnerClientId);
        }

        /*
        playerTank.transform.Find("DuckTankOuter")?.gameObject.SetActive(true);
        */
        
    }

    [ServerRpc]
    private void SetPlayerSpawnServerRpc(ulong clientId)
    {
        GameObject spawnPoint = clientId == 0 ? GameObject.Find("P1Spawn") : GameObject.Find("P2Spawn");
        transform.position = spawnPoint.transform.position;
    }

    private void SetupVirtualCam()
    {
        if (!IsOwner) return;

        virtualCam = FindObjectOfType<CinemachineVirtualCamera>();
        virtualCam.Follow = transform;
    }

    [ServerRpc]
    void SpawnPlayerTankServerRpc(ulong clientId)
    {
        GameObject spawnPoint = clientId == 0 ? GameObject.Find("P1Spawn") : GameObject.Find("P2Spawn");
        playerTank = clientId == 0 ? Instantiate(P1TankPrefab) : Instantiate(P2TankPrefab);

        NetworkObject netInst = playerTank.GetComponent<NetworkObject>();
        netInst.Spawn(true);

        playerTank.transform.position = spawnPoint.transform.position;
        tankrb = playerTank.GetComponent<Rigidbody2D>();

        barrel = playerTank.transform.Find("Barrel")?.gameObject;
        emptyBarrel = barrel.transform.GetChild(0).gameObject;
        loadedBarrel = barrel.transform.GetChild(1).gameObject;

        meterMask = playerTank.transform.Find("PowerMeter/MeterLevelBG").gameObject;
        missileSpawnPoint = playerTank.transform.Find("Barrel/SpawnPoint").transform;
    }

    [ServerRpc]
    void ResetTankServerRpc(ulong clientId)
    {
        GameObject spawnPoint = clientId == 0 ? GameObject.Find("P1Spawn") : GameObject.Find("P2Spawn");
        string tank = clientId == 0 ? "P1Tank(Clone)" : "P2Tank(Clone)";
        playerTank.transform.position = spawnPoint.transform.position;
        playerTank.GetComponent<TankHealthManager>().ResetHealth(tank);
    }

    void Start()
    {
        /*

        emptyBarrel = barrel.transform.GetChild(0).gameObject;
        loadedBarrel = barrel.transform.GetChild(1).gameObject;

        loadedBarrel.SetActive(false);
        */
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.O) && nearDriverSeat && !inCar) {
            inCar = true;
            EnterTankServerRpc();
        } 
        else if (Input.GetKeyDown(KeyCode.O) && inCar) {
            inCar = false;
            ExitTankServerRpc();
        }
        else if (Input.GetKeyDown(KeyCode.O) && nearShooter && !inShoot) {
            inShoot = true;
            PlayerMovementServerRpc(false); // disable player movement
        } else if (Input.GetKeyDown(KeyCode.O) && inShoot) {
            inShoot = false;
            PlayerMovementServerRpc(true); // enable player movement
        }
        if (Input.GetKeyDown(KeyCode.P) && nearAmmo) {
            holdingAmmo = true;
        } else if (Input.GetKeyDown(KeyCode.P) && nearShooter && holdingAmmo && loadedAmmo < 2) {
            holdingAmmo = false;
            loadedAmmo += 1;
            missileLoadSound.Play();
            //emptyBarrel.SetActive(false);
            //loadedBarrel.SetActive(true);
        }

        // Movement handling
        float move = 0f;
        if (Input.GetKey(KeyCode.A)) move = -1f;
        if (Input.GetKey(KeyCode.D)) move = 1f;

        if (inCar)
        {
            if (!tankMoveSound.isPlaying && move != 0) 
                tankMoveSound.Play();
            else if (move == 0) 
                tankMoveSound.Stop();

            MoveTankServerRpc(move);
        }
        else if (inShoot)
        {
            if (!barrelMoveSound.isPlaying && move != 0) 
                barrelMoveSound.Play();
            else if (move == 0) 
                barrelMoveSound.Stop();
            MoveBarrelServerRpc(move);
            HandleShooting();
        }
        else {
            MovePlayerServerRpc(move); 
        }
    }

    [ServerRpc]
    void PlayerMovementServerRpc(bool value)
    {
        rb.simulated = value;
    }

    [ServerRpc]
    void MovePlayerServerRpc(float move) 
    {
        if (rb == null) return;

        rb.velocity = new Vector2(move * 3f, rb.velocity.y);
    }

    [ServerRpc]
    void MoveTankServerRpc(float move)
    {
        tankrb.velocity = new Vector2(move * 3f, tankrb.velocity.y);
    }

    [ServerRpc]
    void MoveBarrelServerRpc(float move)
    {
        // 0.8f is hardcoded rotation speed
        barrel.transform.Rotate(0, 0, 0.8f * -move);

        // make sure barrel cant go below a certain angle (into the tank)
        if (barrel.transform.eulerAngles.z < 4) {
            barrel.transform.Rotate(0, 0, 0.8f * -move);
        } else if (barrel.transform.eulerAngles.z > 178) {
            barrel.transform.Rotate(0, 0, 0.8f * move);
        }
    }

    [ServerRpc]
    void EnterTankServerRpc()
    {
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        transform.SetParent(playerTank.transform);
        transform.localPosition = Vector3.zero; 
    }

    [ServerRpc]
    void ExitTankServerRpc()
    {
        transform.SetParent(null);
        rb.simulated = true;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "DriverInteract") {
            nearDriverSeat = true;
        } else if (collision.gameObject.name == "ShootInteract") {
            nearShooter = true;
        } else if (collision.gameObject.name == "AmmoInteract") {
            nearAmmo = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.name == "DriverInteract") {
            nearDriverSeat = false;
        } else if (collision.gameObject.name == "ShootInteract") {
            nearShooter = false;
        } else if (collision.gameObject.name == "AmmoInteract") {
            nearAmmo = false;
        }
    }

    private void HandleShooting() {
        if (Input.GetKeyDown(KeyCode.W) && missilePowerLevel < 10) {
            missilePowerLevel += 1;
            UpdateMeterServerRpc(missilePowerLevel);
        } else if (Input.GetKeyDown(KeyCode.S) && missilePowerLevel > 1) {
            missilePowerLevel -= 1;
            UpdateMeterServerRpc(missilePowerLevel);
        }

        if (Input.GetKeyDown(KeyCode.Space) && loadedAmmo > 0) {
            ShootMissileServerRpc(missilePowerLevel);
            loadedAmmo -= 1;

            missileFireSound.Play();
        }
    }

    [ServerRpc]
    private void UpdateMeterServerRpc(float missilePowerLevel) {
        float scale = (10 - missilePowerLevel) * 0.03f;
        float yValue = -0.5f*scale + 0.085f;
        

        // Change the Y position correctly
        Vector3 newPosition = meterMask.transform.localPosition;
        newPosition.y = yValue;
        meterMask.transform.localPosition = newPosition;
        

        // Change the Y scale correctly
        Vector3 newScale = meterMask.transform.localScale;
        newScale.y = scale;
        meterMask.transform.localScale = newScale;
    }

    [ServerRpc]
    private void ShootMissileServerRpc(float missilePowerLevel)
    {
        missileInst = Instantiate(missilePrefab, missileSpawnPoint.position, barrel.transform.rotation);
        missileInst.GetComponent<MissileBehaviour>().SetBulletSpeed(missilePowerLevel);
        missileInst.GetComponent<NetworkObject>().Spawn();
        
    }

 


    public void ResetPlayer()
    {
        if (!IsOwner) return;

        if (inCar)
        {
            ExitTankServerRpc();
            inCar = false;
        }
        else if (inShoot)
        {
            inShoot = false;
            PlayerMovementServerRpc(true);
        }

        SetPlayerSpawnServerRpc(OwnerClientId);
        ResetTankServerRpc(OwnerClientId);
    }
}
