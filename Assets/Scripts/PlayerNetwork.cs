using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;

public class PlayerNetwork : NetworkBehaviour
{
    // variables for movement and camera
    private CinemachineVirtualCamera virtualCam;
    private Rigidbody2D rb;

    // used for placing player in corresponding tank
    [SerializeField] private GameObject P1TankPrefab;
    [SerializeField] private GameObject P2TankPrefab;
    private GameObject playerTank;

    // used for tank movement
    private Rigidbody2D tankrb;
    private GameObject duckTankOuter;

    // used for tank barrel movement
    private GameObject barrel;
    private GameObject emptyBarrel;
    private GameObject loadedBarrel;

    // used for visualizing missile power level
    private GameObject meterMask;

    // used for spawning/shooting missile projectiles
    [SerializeField] private GameObject missilePrefab;
    private GameObject missileInst;
    private Transform missileSpawnPoint;
    private int missilePowerLevel = 9;
    private int loadedAmmo = 0;

    // these interactable variables are only to be kept locally, never modified/checked over server
    private bool nearDriverSeat = false;
    private bool nearShooter = false;
    private bool nearAmmo = false;
    private bool inCar = false;
    private bool inShoot = false;
    private bool holdingAmmo = false;

    // sound sources related to player/tank
    [SerializeField] private AudioSource barrelMoveSound;
    [SerializeField] private AudioSource explosionSound;
    [SerializeField] private AudioSource missileFireSound;
    [SerializeField] private AudioSource tankMoveSound;
    [SerializeField] private AudioSource missileLoadSound;


    public override void OnNetworkSpawn()
    {
        // placed outside of the IsOwner check to allow the server to control player movement
        rb = GetComponent<Rigidbody2D>();

        // spawn player, tie the virtual cam to them and give them their tank
        if (IsOwner) {    
            SetPlayerSpawnServerRpc(OwnerClientId);
            SetupVirtualCam();
            SpawnPlayerTankServerRpc(OwnerClientId);
        }

        /*
        playerTank.transform.Find("DuckTankOuter")?.gameObject.SetActive(true);
        */
        
    }

    // sets player to their corresponding spawn point on map
    [ServerRpc]
    private void SetPlayerSpawnServerRpc(ulong clientId)
    {
        GameObject spawnPoint = clientId == 0 ? GameObject.Find("P1Spawn") : GameObject.Find("P2Spawn");
        transform.position = spawnPoint.transform.position;
    }

    // ties player to virtual cam (cinemachine)
    private void SetupVirtualCam()
    {
        if (!IsOwner) return;

        virtualCam = FindObjectOfType<CinemachineVirtualCamera>();
        virtualCam.Follow = transform;
    }

    [ServerRpc]
    void SpawnPlayerTankServerRpc(ulong clientId)
    {
        // get the spawn point based on if it's player1 (host) or player2 (client)
        GameObject spawnPoint = clientId == 0 ? GameObject.Find("P1Spawn") : GameObject.Find("P2Spawn");
        playerTank = clientId == 0 ? Instantiate(P1TankPrefab) : Instantiate(P2TankPrefab);

        // spawn in tank prefab
        NetworkObject netInst = playerTank.GetComponent<NetworkObject>();
        netInst.Spawn(true);

        // place the tank at spawn (player will start inside the tank)
        playerTank.transform.position = spawnPoint.transform.position;
        tankrb = playerTank.GetComponent<Rigidbody2D>(); // used for tank movement later

        // initialize barrel/components for aim movement later
        barrel = playerTank.transform.Find("Barrel")?.gameObject;
        emptyBarrel = barrel.transform.GetChild(0).gameObject;
        loadedBarrel = barrel.transform.GetChild(1).gameObject;

        // initialize mask to visualize missile power level
        meterMask = playerTank.transform.Find("PowerMeter/MeterLevelBG").gameObject;

        // initialize spawn point to later make sure missiles shoot from barrel end 
        missileSpawnPoint = playerTank.transform.Find("Barrel/SpawnPoint").transform;
    }

    [ServerRpc]
    void ResetTankServerRpc(ulong clientId)
    {
        // gets correlating tank and resets it back to spawn point
        GameObject spawnPoint = clientId == 0 ? GameObject.Find("P1Spawn") : GameObject.Find("P2Spawn");
        string tank = clientId == 0 ? "P1Tank(Clone)" : "P2Tank(Clone)";
        playerTank.transform.position = spawnPoint.transform.position;

        // resets tank health back to 100
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
        // only allow owner to handle inputs from player
        if (!IsOwner) return;

        // if player presses O and is near driver seat then start driving tank (disables player (not tank) movement)
        if (Input.GetKeyDown(KeyCode.O) && nearDriverSeat && !inCar) {
            inCar = true;
            EnterTankServerRpc();
        } 
        // if player is already in the seat and presses O, stop driving tank
        else if (Input.GetKeyDown(KeyCode.O) && inCar) {
            inCar = false;
            ExitTankServerRpc();
        }
        // if player presses O and near shooting spot, then let them start aiming barrel
        else if (Input.GetKeyDown(KeyCode.O) && nearShooter && !inShoot) {
            inShoot = true;
            PlayerMovementServerRpc(false); // disable player movement
        }
        // if player is already in shooting seat and presses O, let them off 
        else if (Input.GetKeyDown(KeyCode.O) && inShoot) {
            inShoot = false;
            PlayerMovementServerRpc(true); // enable player movement
        }

        // if user is near ammo spot and presses P, pick up 1 ammo (player only holds one at a time)
        if (Input.GetKeyDown(KeyCode.P) && nearAmmo) {
            holdingAmmo = true;
        }
        // if user is near shooter seat and holds an ammo, load the missile (tank can load 2 missiles max)
        else if (Input.GetKeyDown(KeyCode.P) && nearShooter && holdingAmmo && loadedAmmo < 2) {
            holdingAmmo = false;
            loadedAmmo += 1;
            missileLoadSound.Play();
            //emptyBarrel.SetActive(false);
            //loadedBarrel.SetActive(true);
        }

        // Movement input handling
        float move = 0f;
        if (Input.GetKey(KeyCode.A)) move = -1f;
        if (Input.GetKey(KeyCode.D)) move = 1f;

        // if player in the tank seat, use input for moving tank (A and D)
        if (inCar)
        {
            if (!tankMoveSound.isPlaying && move != 0) 
                tankMoveSound.Play();
            else if (move == 0) 
                tankMoveSound.Stop();

            MoveTankServerRpc(move);
        }
        // if player is in shooting seat, use input for barrel aiming (A and D) and power level (W and S)
        else if (inShoot)
        {
            if (!barrelMoveSound.isPlaying && move != 0) 
                barrelMoveSound.Play();
            else if (move == 0) 
                barrelMoveSound.Stop();
            MoveBarrelServerRpc(move); // syncs barrel movement across clients
            HandleShooting(); // handle firing a missile and setting power level
        }
        // use inputs to move player (server-authorative so rpc used)
        else {
            MovePlayerServerRpc(move); 
        }
    }

    // enables/disables player movement
    [ServerRpc]
    void PlayerMovementServerRpc(bool value)
    {
        rb.simulated = value;
    }

    // moves player...
    [ServerRpc]
    void MovePlayerServerRpc(float move) 
    {
        if (rb == null) return;

        rb.velocity = new Vector2(move * 3f, rb.velocity.y);
    }


    // moves tank...
    [ServerRpc]
    void MoveTankServerRpc(float move)
    {
        tankrb.velocity = new Vector2(move * 3f, tankrb.velocity.y);
    }

    // rotates the barrel based on movement input 
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

    // begins tank movement
    [ServerRpc]
    void EnterTankServerRpc()
    {
        // disables the player from moving while driving tank
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        // ties player to tank seat
        transform.SetParent(playerTank.transform);
        transform.localPosition = Vector3.zero; 
    }

    // ends tank movement
    [ServerRpc]
    void ExitTankServerRpc()
    {
        // unties player from tank seat and allows movement again
        transform.SetParent(null);
        rb.simulated = true;
    }

    // if player is near an interact, set the correlating bool
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

    // if player leaves an interact, unset the correlating bool
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

    // handles changing power level and firing missile
    private void HandleShooting() {
        // if within max power level range (1-10) and player presses W/S, increase/decrease power level
        if (Input.GetKeyDown(KeyCode.W) && missilePowerLevel < 10) {
            missilePowerLevel += 1;
            UpdateMeterServerRpc(missilePowerLevel); // updated across clients to show what power level used
        } else if (Input.GetKeyDown(KeyCode.S) && missilePowerLevel > 1) {
            missilePowerLevel -= 1;
            UpdateMeterServerRpc(missilePowerLevel);
        }

        // if there is ammo loaded and player presses space, then fire a missile (spawn/movement handled by server)
        if (Input.GetKeyDown(KeyCode.Space) && loadedAmmo > 0) {
            ShootMissileServerRpc(missilePowerLevel);
            loadedAmmo -= 1;

            missileFireSound.Play();
        }
    }

    // updates the power level meter in the tank
    [ServerRpc]
    private void UpdateMeterServerRpc(float missilePowerLevel) {
        // couldn't figure out real masking so I did math to scale/move an overlay to hide/show power level as it changes
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
