using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileBehaviour : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 15f;
    private Rigidbody2D rb;

    void Start() {
        rb = GetComponent<Rigidbody2D>();

        SetStraightVelocity();
    }

    private void FixedUpdate() {
        float angleDeg = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angleDeg);
    }

    public void SetBulletSpeed(float speed) {
        bulletSpeed = speed;
    }


    private void SetStraightVelocity() {
        rb.velocity = transform.right * bulletSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision) {
        Destroy(gameObject);
    }
}
