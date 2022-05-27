using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Gun : MonoBehaviour
{
    public GameObject bullet;

    public float shootForce;

    public float timeBetweenShooting, spread, timeBetweenShots;
    public bool allowButtonHold;

    public Camera cam;
    public Transform attackPoint;
    public GameObject muzzleFlash;

    public float recoilForce;

    private bool readyToShoot, airShooting, groundShooting;
    private bool allowInvoke = true;

    [HideInInspector]
    public List<Vector3> airExplosionPoints = new List<Vector3>();
    public List<Vector3> groundExplosionPoints = new List<Vector3>();

    private void Awake() {
        readyToShoot = true;
    }

    private void Update() {
        myInput();
    }

    private void myInput() {
        if(allowButtonHold) airShooting = Input.GetKey(KeyCode.Mouse0);
        else airShooting = Input.GetKeyDown(KeyCode.Mouse0);

        if(allowButtonHold) groundShooting = Input.GetKey(KeyCode.Mouse1);
        else groundShooting = Input.GetKeyDown(KeyCode.Mouse1);

        if(readyToShoot && (airShooting || groundShooting)) {
            shoot(airShooting);
        }
    }

    private void shoot(bool isAirShot) {
        readyToShoot = false;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint;
        if(Physics.Raycast(ray, out hit)) {
            targetPoint = hit.point;
        } else {
            targetPoint = ray.GetPoint(75);
        }

        Vector3 directionNoSpread = targetPoint - attackPoint.position;

        float xSpread = Random.Range(-spread, spread);
        float ySpread = Random.Range(-spread, spread);

        Vector3 direction = directionNoSpread + new Vector3(xSpread, ySpread, 0);

        GameObject currentBullet = Instantiate(bullet, attackPoint.position, Quaternion.identity);
        currentBullet.GetComponent<Bullet>().gun = this;
        currentBullet.GetComponent<Bullet>().isAirBullet = isAirShot;
        currentBullet.transform.forward = direction.normalized;

        currentBullet.GetComponent<Rigidbody>().AddForce(direction.normalized * shootForce, ForceMode.Impulse);

        //rb.AddForce(-direction.normalized * recoilForce, ForceMode.Impulse);

        if(muzzleFlash != null) {
            Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);
        }

        if (allowInvoke) {
            allowInvoke = false;
            Invoke("ResetShot", timeBetweenShooting);
        }
    }

    private void ResetShot() {
        allowInvoke = true;
        readyToShoot = true;
    }
}
