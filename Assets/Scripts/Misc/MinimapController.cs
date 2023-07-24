using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class MinimapController : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    private GameObject playerMarker;

    private GameStateManager manager;

    private List<GameObject> boidMarkers = new List<GameObject>();
    bool firstUpdate = true;

    void Start()
    {
        manager = GameObject.FindGameObjectWithTag("TerrainGenerator").GetComponent<GameStateManager>();
        
    }

    void Update()
    {
        if(firstUpdate) {
            initiateMarkers();
            firstUpdate = false;
        }
        transform.Rotate(new Vector3(0, Input.GetAxis("Pan") / 2, 0));
        checkForDeadMarkers();
        moveMarkers();
    }

    private void initiateMarkers()
    {
        foreach (Transform boid in manager.boids) {
            boidMarkers.Add(Utils.instantiate(enemyPrefab, transform.position, transform, Color.red, "Minimap"));
        }

        playerMarker = Utils.instantiate(playerPrefab, transform.position, transform, Color.green, "Minimap");
    }


    private void checkForDeadMarkers() 
    {
        if(boidMarkers.Count > manager.boids.Count) {
            Destroy(boidMarkers[boidMarkers.Count-1]);
            boidMarkers.RemoveAt(boidMarkers.Count-1);
        }
    }

    private void moveMarkers()
    {
        for (int i = 0; i < manager.boids.Count; i++)
        {
            Vector3 boidPos = (manager.boids[i].localPosition - manager.player.localPosition) / 20;

            float rectRadius = 7;
            // rectangle clamp
            if (Mathf.Abs(boidPos.x) > rectRadius) boidPos.x = (boidPos.x > 0 ? 1 : -1) * rectRadius;
            if (Mathf.Abs(boidPos.y) > rectRadius) boidPos.y = (boidPos.y > 0 ? 1 : -1) * rectRadius;
            if (Mathf.Abs(boidPos.z) > rectRadius) boidPos.z = (boidPos.z > 0 ? 1 : -1) * rectRadius;

            boidMarkers[i].transform.localPosition = boidPos;
            boidMarkers[i].transform.localRotation = manager.boids[i].rotation;
        }

        playerMarker.transform.localRotation = manager.player.rotation;
        playerMarker.transform.Rotate(new Vector3(180, 0, 0));
    }
}
