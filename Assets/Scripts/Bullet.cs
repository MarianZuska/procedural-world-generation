using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody rb;
    public GameObject explosion;
    //public LayerMask whatIsEnemies;

    [Range(0,1)]
    public float bounciness;
    public bool useGravity;

    public int maxCollisions;
    public float maxLifetime;
    public bool explodeOnTouch = true;
    
    int collisions;
    PhysicMaterial physicsMat;


    private void Update() {
        if(collisions > maxCollisions) explode();

        maxLifetime -= Time.deltaTime;
        if(maxLifetime <= 0) explode();

    }

    private void explode() {
        if(explosion != null) Instantiate(explosion, transform.position, Quaternion.identity);

        Invoke("DestroySelf", 0.05f);
    }

    private void DestroySelf() {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision other) {
        collisions++;

    }

    private void Setup() {
        physicsMat = new PhysicMaterial();
        physicsMat.bounciness = bounciness;
        physicsMat.frictionCombine = PhysicMaterialCombine.Minimum;
        physicsMat.bounceCombine = PhysicMaterialCombine.Maximum;

        GetComponent<SphereCollider>().material = physicsMat;

        rb.useGravity = useGravity;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3);
    }
}
