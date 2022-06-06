using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int enemyCount = 20;
    public float maxSpawnDist = 200;
    public float minDistToPlayer = 20;
    
    public Transform playerTransform;
    public GameObject enemyPrefab;

    private void Start() {
        for(int i = 0; i < enemyCount; i++) {
            // maxSpawnDist is technically not a distance here, since (200, 200) has a higher distance than 200
            Vector3 spawnPos = randomVec(-maxSpawnDist, maxSpawnDist);
            i = 20;
            while(Vector2.Distance(spawnPos, playerTransform.position) < minDistToPlayer && i-- > 0) {
                spawnPos = randomVec(-maxSpawnDist, maxSpawnDist);
            }
            GameObject enemy = Instantiate(enemyPrefab);
            enemy.transform.position = spawnPos;
            enemy.transform.localScale *= 10;
        }
    }

    private Vector3 randomVec(float minValue, float maxValue) {
        float x = Random.Range(minValue, maxValue);
        float y = Random.Range(minValue, maxValue);
        float z = Random.Range(minValue, maxValue);

        return new Vector3(x,y,z);
    }
}
