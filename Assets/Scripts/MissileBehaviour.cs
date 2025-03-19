using Unity.Netcode;
using UnityEngine;

public class MissileBehaviour : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 15f;
    private Rigidbody2D rb;

    public override void OnNetworkSpawn() 
    {
        if (IsServer) 
        {
            rb = GetComponent<Rigidbody2D>();
            SetStraightVelocity();
        }
    }

    private void FixedUpdate() 
    {
        if (rb == null) return;

        float angleDeg = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angleDeg);
    }

    public void SetBulletSpeed(float speed) 
    {
        bulletSpeed = speed;
    }

    private void SetStraightVelocity() 
    {
        rb.velocity = transform.right * bulletSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision) 
    {
        if (IsServer) 
        {
            if (collision.gameObject.tag == "tank") {
                var healthScript = collision.gameObject.GetComponent<TankHealthManager>();
                healthScript.missileHit(collision.gameObject.name);
            }
            Destroy(gameObject);
        }
    }
}
