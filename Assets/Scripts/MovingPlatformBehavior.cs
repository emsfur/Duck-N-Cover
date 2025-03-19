using UnityEngine;
using Unity.Netcode;

public class MovingPlatformBehavior : NetworkBehaviour
{
    public float moveSpeed = 2f;
    public float startY;
    public float stopY;

    private int direction = 1; // 1 = Up, -1 = Down
    private Rigidbody2D rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!IsServer) return;

        if (transform.position.y > stopY && direction == 1)
        {
            direction *= -1;
        } 
        else if (transform.position.y < startY && direction == -1)
        {
            direction *= -1;
        }

        if (direction == 1)
        {
            movePlatformUpServerRpc();
        }
        else if (direction == -1)
        {
            movePlatformDownServerRpc();
        }
    }

    [ServerRpc]
    void movePlatformDownServerRpc()
    {
        rb.velocity = new Vector2(rb.velocity.x, -moveSpeed);
    }

    [ServerRpc]
    void movePlatformUpServerRpc()
    {
        rb.velocity = new Vector2(rb.velocity.x, moveSpeed);
    }
}
