using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Rigidbody rb;
    public GameObject explosion;

    [HideInInspector]
    public Gun gun;
    public bool isAirBullet = true;
    public ComputeMaster computeMaster;
    //public LayerMask whatIsEnemies;

    [Range(0,1)]
    public float bounciness;
    public bool useGravity;

    public int maxCollisions;
    public float maxLifetime;
    public bool explodeOnTouch = true;

    private bool hasExploded = false;
    private int collisions;
    private PhysicMaterial physicsMat;


    private void Update() {
        maxLifetime -= Time.deltaTime;
        if(collisions > maxCollisions || maxLifetime <= 0) explode();
    }

    private void explode() {
        if(hasExploded) return;
        hasExploded = true;

        if(explosion != null) Instantiate(explosion, transform.position, Quaternion.identity);

        if(computeMaster != null) {
            Debug.Log("Explosion");
            computeMaster.createSphere(transform.position, isAirBullet, true);
        }
        //if(isAirBullet) gun.airExplosionPoints.Add(transform.position);
        //else gun.groundExplosionPoints.Add(transform.position);
        
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
        Gizmos.DrawWireSphere(transform.position, 20);
    }
}
