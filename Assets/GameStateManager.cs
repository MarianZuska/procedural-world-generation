using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    float score = 0f;
    public TextMeshProUGUI scoreGO;


    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    public Transform player;
    public List<Transform> boids;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        scoreGO.text = score.ToString();
        boids = new List<Transform>();

        GameObject[] boidGOs = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject boid in boidGOs) {
            boids.Add(boid.transform);
        }
    }

    public void increaseScore(int increment) {
        score += increment;
        scoreGO.text = score.ToString();
    }
}
