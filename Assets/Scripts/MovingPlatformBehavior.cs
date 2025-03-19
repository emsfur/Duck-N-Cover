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

        // if the platform was going up and hits the stopY value, then start going down
        if (transform.position.y > stopY && direction == 1)
        {
            direction *= -1;
        } 
        // if the platform was going down and hits the startY value, then start going up
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

    // movement itself is handled by server
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
