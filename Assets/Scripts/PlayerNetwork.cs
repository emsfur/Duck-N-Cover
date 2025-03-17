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

    /*
    private GameObject tank;
    public GameObject barrel;
    [SerializeField] private GameObject missile;
    [SerializeField] private Transform missileSpawnPoint;

    private int missilePowerLevel = 9;
    [SerializeField] private GameObject meterMask;

    private GameObject missileInst;
    private GameObject emptyBarrel;
    private GameObject loadedBarrel;

    private Rigidbody2D tankrb;
    */
    

    // these interactable variables are only to be kept locally, never modified/checked over server
    private bool nearDriverSeat = false;
    private bool nearShooter = false;
    private bool nearAmmo = false;
    private bool inCar = false;
    private bool inShoot = false;
    private bool holdingAmmo = false;

    /*
    private float rotationSpeed = 0.8f;
    private int loadedAmmo = 0;
    */

    public override void OnNetworkSpawn()
    {
        // placed outside of the IsOwner check to allow the server to control player movement
        rb = GetComponent<Rigidbody2D>();

        if (IsOwner) {    
            SetPlayerSpawnServerRpc(OwnerClientId);
            SetupVirtualCam();
            SpawnPlayerTankServerRpc(OwnerClientId);
        }

        if (IsOwner)
        {
            // Make sure the playerTank is ready before interacting with it
            StartCoroutine(WaitForTankAndSetup());
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
    }

    // Wait for the tank to be fully set up before interacting with it
    private IEnumerator WaitForTankAndSetup()
    {
        while (playerTank == null)
        {
            yield return null; // Wait until playerTank is assigned
        }

        // Once the tank is available, we can safely access it
        barrel = playerTank.transform.Find("Barrel")?.gameObject;
        emptyBarrel = barrel.transform.GetChild(0).gameObject;
        loadedBarrel = barrel.transform.GetChild(1).gameObject;

        playerTank.transform.GetChild(0).gameObject.SetActive(false); // Set the DuckTankOuter inactive initially

        // Additional setup if needed
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

        if (Input.GetKeyDown(KeyCode.O) && nearDriverSeat) {
            inCar = true;
            EnterTankServerRpc();
        } 
        else if (Input.GetKeyDown(KeyCode.O) && inCar) {
            inCar = false;
            ExitTankServerRpc();
        } 
        /*
        else if (Input.GetKeyDown(KeyCode.O) && nearShooter) {
            inShoot = true;
            rb.simulated = false; 
        } else if (Input.GetKeyDown(KeyCode.O) && inShoot) {
            inShoot = false;
            rb.simulated = true; 
        }
        
        if (Input.GetKeyDown(KeyCode.P) && nearAmmo) {
            holdingAmmo = true;
            Debug.Log("Holding Ammo");
        } else if (Input.GetKeyDown(KeyCode.P) && nearShooter && holdingAmmo && loadedAmmo < 2) {
            holdingAmmo = false;
            loadedAmmo += 1;

            emptyBarrel.SetActive(false);
            loadedBarrel.SetActive(true);
        }

        */

        // Movement handling
        float move = 0f;
        if (Input.GetKey(KeyCode.A)) move = -1f;
        if (Input.GetKey(KeyCode.D)) move = 1f;

        if (inCar)
        {
            MoveTankServerRpc(move);
        }
        else {
            MovePlayerServerRpc(move); 
        }
        /*
        if (inCar) {
            // Only move the tank
            tankrb.velocity = new Vector2(move * moveSpeed, tankrb.velocity.y);
        } else if (inShoot) {
            // Debug.Log(barrel.transform.eulerAngles.z);
            barrel.transform.Rotate(0, 0, rotationSpeed * -move);

            if (barrel.transform.eulerAngles.z < 4) {
                barrel.transform.Rotate(0, 0, rotationSpeed * -move);
            } else if (barrel.transform.eulerAngles.z > 178) {
                barrel.transform.Rotate(0, 0, rotationSpeed * move);
            }

            HandleShooting();

        } else {
            // Only move the player
            rb.velocity = new Vector2(move * moveSpeed, rb.velocity.y);
        }

        */
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
    void EnterTankServerRpc()
    {
        rb.velocity = Vector2.zero;          // Stop player motion
        rb.simulated = false;                // Disable player physics
        transform.SetParent(playerTank.transform); // Lock to tank
        transform.localPosition = Vector3.zero; // Ensure exact position
    }

    [ServerRpc]
    void ExitTankServerRpc()
    {
        transform.SetParent(null);           // Detach from tank
        rb.simulated = true;                 // Re-enable physics
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

    /*
    private void HandleShooting() {
        if (Input.GetKeyDown(KeyCode.W) && missilePowerLevel < 10) {
            updateMeter(+1);
        } else if (Input.GetKeyDown(KeyCode.S) && missilePowerLevel > 1) {
            updateMeter(-1);
        }

        if (Input.GetKeyDown(KeyCode.Space) && loadedAmmo > 0) {
            missileInst = Instantiate(missile, missileSpawnPoint.position, barrel.transform.rotation);

            missileInst.GetComponent<MissileBehaviour>().SetBulletSpeed(missilePowerLevel);
            loadedAmmo -= 1;
        }
    }

    private void updateMeter(int delta) {
        missilePowerLevel += delta;
        Debug.Log("power level: " + missilePowerLevel);

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
    */
}
