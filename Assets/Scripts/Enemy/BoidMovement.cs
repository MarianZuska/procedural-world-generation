using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidMovement : MonoBehaviour
{
    public float minSpeed = 10;
    public float maxSpeed = 12;
    
    public bool showDebugLines = true;

    private GeneratorHelper generatorHelper;
    private Vector3 velocity;

    private float fowardAcceleration = 5f;
    private float randomAcceleration = 0.15f;
    private float antiSurfaceAcceleration = 0.2f;

    void Start() {
        generatorHelper = GameObject.FindGameObjectWithTag("TerrainGenerator").GetComponent<GeneratorHelper>();

        velocity = transform.forward * (minSpeed + maxSpeed) / 2f;
    }

    void FixedUpdate()
    {
        bool moving = generatorHelper.isInsideBoundingBox(transform.position);

        if (moving) {
            Vector3 acceleration = Vector3.zero;

            Vector3 noObstacleDir = forwardWithoutObstacle();
            Vector3 noiseDir = randomPNoiseVector();
            Vector3 closestSurfaceDir = closestSurfaceDirection();

            acceleration += noObstacleDir * fowardAcceleration;
            acceleration += noiseDir * randomAcceleration;
            acceleration += -1 * closestSurfaceDir * antiSurfaceAcceleration;

            if (showDebugLines) {
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 10, Color.white);
                Debug.DrawRay(transform.position, acceleration * 10, Color.red);
                Debug.DrawRay(transform.position, noObstacleDir * 10, Color.green);
                Debug.DrawRay(transform.position, -closestSurfaceDir * 5, Color.yellow);
            }
            
            velocity += acceleration;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            velocity = ClampMagnitude(velocity, maxSpeed, minSpeed);

            transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);
            //Debug.DrawRay(transform.position, getHit() * 100, Color.red);
            transform.position += velocity * Time.fixedDeltaTime;
        }
    }

    // non-seaded random vector using Perlin Noise.
    Vector3 randomPNoiseVector() {
        float x = Mathf.PerlinNoise(Time.realtimeSinceStartup/2, -12345) * 2 - 1;
        float y = Mathf.PerlinNoise(Time.realtimeSinceStartup/2, -23456) * 2 - 1;
        float z = Mathf.PerlinNoise(Time.realtimeSinceStartup/2, -34567) * 2 - 1;

        Vector2 vec = new Vector3(x,y,z);
        return vec.normalized;
    }

    // computes the vector that is closest to the forward direction where no obstacle is found
    // if all rays hit obstacles, the forward direction is returned
    Vector3 forwardWithoutObstacle() {
        float size = 2.4f;
        int numPoints = 70;
        // set turn fraction to golden ratio (see Sebastian Lagues Boid video)
        float turnFraction = 0.1618f; 

        for(int i = 0; i < numPoints; i++) {
            float dist = (i * size) / (numPoints - 1f);
            float angle = 2 * Mathf.PI * turnFraction * i;
            
            float x = dist * Mathf.Cos(angle);
            float y = dist * Mathf.Sin(angle);

            Vector3 dir = new Vector3(x,y,2);
            dir = dir.normalized;
            dir = transform.TransformDirection(dir);
            Ray ray = new Ray (transform.position, dir);
            if(!Physics.Linecast(transform.position, transform.position + dir * 20)) {
                return dir;
            }
        }
        
        return transform.forward;
    }

    //returns the direction to the nearest surface
    Vector3 closestSurfaceDirection() {
        float minDist = 1e9f;
        Vector3 minDistDirection = Vector3.zero;

        for(int x = -1; x <= 1; x++) {
            for(int y = -1; y <= 1; y++) {
                for(int z = -1; z <= 1; z++) {
                    if(x == 0 && y == 0 && z == 0) continue;

                    Vector3 dir = new Vector3(x,y,z);
                    dir = dir.normalized;
                    
                    RaycastHit hit;
                    Ray ray = new Ray(transform.position, dir);

                    if(Physics.Linecast(transform.position, transform.position + dir * 5, out hit)) {
                        if(hit.distance < minDist) {
                            minDist = hit.distance;
                            minDistDirection = dir;
                        }
                    }
                }
            }
        }

        return minDistDirection;
    }

    public static Vector3 ClampMagnitude(Vector3 v, float max, float min)
    {
        double sqrMagnitude = v.sqrMagnitude;

        if(sqrMagnitude > (double) max * (double) max) return v.normalized * max;
        else if(sqrMagnitude < (double) min * (double) min) return v.normalized * min;
        else return v;
    }
}
