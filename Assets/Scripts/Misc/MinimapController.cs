using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapController : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    private Transform player;
    private List<Transform> boids;

    private List<GameObject> boidMarkers;
    private GameObject playerMarker;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        GameObject[] boidGOs = GameObject.FindGameObjectsWithTag("Enemy");

        boids = new List<Transform>();
        boidMarkers = new List<GameObject>();
        foreach (GameObject boid in boidGOs) {
            boids.Add(boid.transform);

            GameObject boidSphere = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            boidSphere.transform.parent = transform;
            boidSphere.layer = LayerMask.NameToLayer("Minimap");
            boidSphere.GetComponent<Renderer>().material.color = Color.red;
            boidMarkers.Add(boidSphere);
        }


        playerMarker = Instantiate(playerPrefab, transform.position, Quaternion.identity);
        //playerMarker.transform.parent = transform;
        playerMarker.layer = LayerMask.NameToLayer("Minimap");
        //playerMarker.transform.Rotate(new Vector3(90,0,0));
        playerMarker.GetComponent<Renderer>().material.color = Color.green;
    }

    void Update()
    {
        // if(Input.GetButtonDown("q")) {
        //     transform.Rotate(new Vector3(0,2,0));
        // }
        // if(Input.GetButtonDown("e")) {
        //     transform.Rotate(new Vector3(0,2,0));
        // }


        for(int i = 0; i < boids.Count; i++) {
            Vector3 boidPos = (boids[i].localPosition - player.localPosition) / 20;

            Debug.Log(boidPos);
            float rectRadius = 7;
            // rectangle clamp
            if(Mathf.Abs(boidPos.x) > rectRadius) boidPos.x = (boidPos.x > 0 ? 1 : -1) * rectRadius;
            if(Mathf.Abs(boidPos.y) > rectRadius) boidPos.y = (boidPos.y > 0 ? 1 : -1) * rectRadius;
            if(Mathf.Abs(boidPos.z) > rectRadius) boidPos.z = (boidPos.z > 0 ? 1 : -1) * rectRadius;

            boidMarkers[i].transform.localPosition = boidPos;
            boidMarkers[i].transform.rotation = boids[i].rotation;
        }

        playerMarker.transform.rotation = player.rotation;
        playerMarker.transform.Rotate(new Vector3(180,0,0));

    }
}
