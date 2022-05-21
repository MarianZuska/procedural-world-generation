using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeMaster : MonoBehaviour
{
    public ComputeShader marchingShader;

    public Transform player;
    public float surfaceLevel;

    [Range(1, 1)]
    public float worldZoom = 1f;

    public int numPointsPerAxis;
    public float offsetX = 0f;
    public float offsetY = 0f;
    public float offsetZ = 0f;
    public float perlinZoom = 2f;
    //public int worldScale = 1;

    public int circleRadius = 10;

    public Color lightColor;
    public Color darkColor;

    public GameObject meshHolder;


    private Vector3 lastBeenSector = Vector3.zero;
    private GameObject s;
    private Mesh mesh;
    private Triangle[] tris;
    private float[,,] points;
    private int numVoxelsPerAxis;
    private bool firstFrame;
    private int ciclesCount = 0;
    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer triCountBuffer;
    private ComputeBuffer circleBuffer;

    private List<Vector3> circles;

    private GameObject curMeshHolder;
    private Vector3 offsetVec;
    private Vector3 minCornerPos;
    private Vector3 currentSector;
    private int maxTriangleCount;
    private Vector3 circle;

    private List<Chunk> meshHolders;

    private Camera camera;

    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    };

    struct Chunk
    {
        public Vector3 position;
        public GameObject meshHolder;
        public bool used;
        public bool destroy;
        public Vector3[] vertices;
        public int[] triangles;
    }

    private void SetShaderParameters(Vector3 minCorner)
    {

        marchingShader.SetBuffer(0, "triangles", trianglesBuffer);

        /*circleBuffer = new ComputeBuffer(circles.Count, sizeof(float)*3, ComputeBufferType.Append);
        trianglesBuffer.SetData(circles);
        marchingShader.SetBuffer(0, "circles", circleBuffer);

        marchingShader.SetInt("circlesCount", circles.Count);*/
        marchingShader.SetFloats("circle", new float[] { circle.x, circle.y, circle.z });
        marchingShader.SetInt("circleRadius", circleRadius);
        marchingShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingShader.SetFloat("surfaceLevel", surfaceLevel);
        marchingShader.SetFloat("perlinZoom", perlinZoom);
        marchingShader.SetFloat("worldZoom", worldZoom);

        float[] pos = new float[3];
        pos[0] = minCorner.x;
        pos[1] = minCorner.y;
        pos[2] = minCorner.z;
        marchingShader.SetFloats("minSectorPos", pos);
        marchingShader.SetFloats("offsetVec", new float[] { offsetX, offsetY, offsetZ });
    }

    //modulo that works for negative numbers
    float mod(float k, float n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    void Start()
    {
        camera = Camera.main;

        int numVoxels = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        maxTriangleCount = numVoxels * 50;
        trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        Debug.Log(maxTriangleCount * 36);

        curMeshHolder = Instantiate(meshHolder, Vector3.zero, Quaternion.identity, transform);

        mesh = new Mesh();
        curMeshHolder.GetComponent<MeshFilter>().sharedMesh = mesh;

        currentSector = Vector3.zero;


        meshHolders = new List<Chunk>();
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    GameObject tmpMeshHolder = Instantiate(meshHolder, Vector3.zero, Quaternion.identity, transform);
                    Mesh curMesh = new Mesh();
                    tmpMeshHolder.GetComponent<MeshFilter>().sharedMesh = curMesh;
                    tmpMeshHolder.GetComponent<MeshCollider>().sharedMesh = curMesh;
                    ///////////////////////////////// DOESNT WORK
                    Chunk cur = new Chunk();
                    cur.meshHolder = tmpMeshHolder;
                    cur.position = Vector3.zero;
                    cur.used = false;
                    cur.destroy = false;
                    meshHolders.Add(cur);
                }
            }
        }
    }

    void Update()
    {
        float startingTime = Time.realtimeSinceStartup;
        float timeInShader = 0;
        float timeInMeshAssembly = 0;
        float timeGettingShaderData = 0;

        //ability
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ciclesCount += 1;
            circle = player.position - new Vector3(10, 10, 10);
            firstFrame = true;

            //redo all meshes if ability cast
            for (int i = 0; i < meshHolders.Count; i++)
            {
                Destroy(meshHolders[i].meshHolder);
            }
            meshHolders = new List<Chunk>();
        }

        Vector3 playerPosition = player.position;
        Vector3 nextSector = new Vector3(playerPosition.x - mod(playerPosition.x, numPointsPerAxis) - numPointsPerAxis,
                                         playerPosition.y - mod(playerPosition.y, numPointsPerAxis) - numPointsPerAxis,
                                         playerPosition.z - mod(playerPosition.z, numPointsPerAxis) - numPointsPerAxis);
        //nextSector /= worldZoom;

        bool sectorChanged = firstFrame || Vector3.Distance(currentSector, nextSector) >= 0.2f;

        //actual cube marching
        if (sectorChanged)
        {
            firstFrame = false;
            currentSector = nextSector;

            List<Vector3> vertsAll = new List<Vector3>();
            List<int> trisAll = new List<int>();

            //"deep copy" meshHolders into oldChunks but set destroy to true
            List<Chunk> oldChunks = new List<Chunk>();
            for (int i = 0; i < meshHolders.Count; i++)
            {
                Chunk chunk = meshHolders[i];
                chunk.destroy = true;
                oldChunks.Add(chunk);
            }

            meshHolders = new List<Chunk>();
            int meshHolderIndex = 0;
            int skippedCount = 0;

            //iterate through all chunks (3x3x3)
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {

                        Vector3 targetCoordinate = currentSector + new Vector3(
                            numPointsPerAxis * x,
                            numPointsPerAxis * y,
                            numPointsPerAxis * z);// /worldZoom;

                        int meshIndex = checkIfMeshExists(oldChunks, targetCoordinate);

                        //mesh already there
                        if (meshIndex > -1)
                        {
                            skippedCount++;
                            meshHolders.Add(oldChunks[meshIndex]);
                        }

                        //new mesh needed
                        else
                        {

                            //Run shader
                            int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / 8.0f);
                            SetShaderParameters(targetCoordinate);
                            float timeBefore = Time.realtimeSinceStartup;
                            marchingShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
                            timeInShader += Time.realtimeSinceStartup - timeBefore;

                            //Get shader data
                            timeBefore = Time.realtimeSinceStartup;

                            // Num of tris
                            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
                            ComputeBuffer.CopyCount(trianglesBuffer, triCountBuffer, 0);
                            int[] triCountArray = { 0 };
                            triCountBuffer.GetData(triCountArray);
                            int numTris = triCountArray[0];
                            triCountBuffer.Release();

                            Debug.Log("Num tris: " + numTris);

                            // Tris data
                            tris = new Triangle[numTris];
                            trianglesBuffer.GetData(tris, 0, 0, numTris);
                            trianglesBuffer.Release();

                            timeGettingShaderData += Time.realtimeSinceStartup - timeBefore;

                            timeBefore = Time.realtimeSinceStartup;



                            // fill mesh 
                            var vertices = new Vector3[numTris * 3];
                            var triangles = new int[numTris * 3];

                            for (int i = 0; i < numTris; i++)
                            {
                                vertices[3 * i + 0] = tris[i].a;
                                vertices[3 * i + 1] = tris[i].b;
                                vertices[3 * i + 2] = tris[i].c;
                                triangles[3 * i + 0] = 3 * i + 0;
                                triangles[3 * i + 1] = 3 * i + 1;
                                triangles[3 * i + 2] = 3 * i + 2;
                            }
                            
                            
                            

                            // draw mesh
                            Mesh newMesh = new Mesh();
                            newMesh.Clear();
                            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                            newMesh.vertices = vertices;
                            newMesh.triangles = triangles;
                            newMesh.RecalculateNormals();
                            //newMesh.RecalculateBounds();
                            //newMesh.RecalculateTangents();
                            //color mesh
                            camera.backgroundColor = darkColor;
                            Color[] colors = new Color[vertices.Length];
                            for (int i = 0; i < vertices.Length; i++)
                                colors[i] = Color.Lerp(lightColor, darkColor, Mathf.PerlinNoise(vertices[i][0]/5f, vertices[i][1]/5f)*2f - 0.5f);
                            newMesh.colors = colors;

                            Chunk chunk = new Chunk();
                            chunk.meshHolder = Instantiate(meshHolder, Vector3.zero, Quaternion.identity, transform);

                            MeshFilter meshFilter = chunk.meshHolder.GetComponent<MeshFilter>();
                            meshFilter.sharedMesh = newMesh;

                            //chunk.meshHolder.GetComponent<MeshCollider>().sharedMesh = newMesh;

                            MeshCollider meshCollider = chunk.meshHolder.GetComponent<MeshCollider>();
                            // force update
                            meshCollider.enabled = false;
                            meshCollider.enabled = true;

                            meshCollider.sharedMesh = null;
                            meshCollider.sharedMesh = newMesh;

                            meshCollider.enabled = false;
                            meshCollider.enabled = true;

                            chunk.destroy = false;
                            chunk.used = true;
                            chunk.position = targetCoordinate;

                            meshHolders.Add(chunk);

                            timeInMeshAssembly += Time.realtimeSinceStartup - timeBefore;

                            trianglesBuffer.Release();
                            trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
                            trianglesBuffer.SetCounterValue(0);
                            triCountBuffer.Release();


                        }
                        meshHolderIndex++;
                    }
                }
            }

            Debug.Log("-------------------");
            Debug.Log("Performance measures:");
            Debug.Log("Time in update overall:" + (Time.realtimeSinceStartup - startingTime));
            Debug.Log("Time in shader: " + timeInShader);
            Debug.Log("Time getting shader data: " + timeGettingShaderData);
            Debug.Log("Time in mesh assembly: " + timeInMeshAssembly);
            Debug.Log("-------------------");
            Debug.Log("");

            //destroy not used:
            for (int i = 0; i < oldChunks.Count; i++)
            {
                if (oldChunks[i].destroy)
                {
                    Destroy(oldChunks[i].meshHolder);
                }
            }

            //updateMesh(vertsAll, trisAll);
            /*lastBeenSector = currentSector;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = nextSector;
            sphere.transform.localScale = new Vector3(1f, 1f, 1f);
            currentSector = nextSector;

            vertices.Clear();
            triangles.Clear();

            int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / 8.0f);

            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    for (int z = -1; z <= 1; z++) {
                        Vector3 cur = currentSector + new Vector3(numPointsPerAxis * x, numPointsPerAxis * y, numPointsPerAxis * z);
                        //start compute shader
                        SetShaderParameters(cur);
                        marchingShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
                    }
                }
            }

            SetShaderParameters(currentSector);
            marchingShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
            
            
            // Get number of triangles in the triangle buffer
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(trianglesBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData(triCountArray);
            int numTris = triCountArray[0];

            Debug.Log("Bytes: " + ((numTris * sizeof(float) * 3 * 3)));
            // Get triangle data from shader
            tris = new Triangle[numTris];
            trianglesBuffer.GetData(tris, 0, 0, numTris);

            //make Mesh
            for (int i = 0; i < numTris; i++) {
                vertices.Add(tris[i].a);
                vertices.Add(tris[i].b);
                vertices.Add(tris[i].c);
                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);
            }

            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            vertices.Clear();
            triangles.Clear();

            trianglesBuffer.Release();
            trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            trianglesBuffer.SetCounterValue(0); // YES FINALLY (hopefully)
        
            

            triCountBuffer.Release();*/

        }
    }

    private int checkIfMeshExists(List<Chunk> oldMeshHolders, Vector3 targetCoordinate)
    {
        float minDist = 100;
        int equalMeshIndex = -1;
        for (int i = 0; i < oldMeshHolders.Count; i++)
        {
            if (oldMeshHolders[i].used)
            {
                var dist = Vector3.Distance(oldMeshHolders[i].position, targetCoordinate);
                if (dist < minDist)
                {
                    minDist = dist;
                }

                if (dist < 0.1f && oldMeshHolders[i].used)
                {
                    //save index
                    equalMeshIndex = i;

                    //set destroy to false
                    Chunk tmp = oldMeshHolders[i];
                    tmp.destroy = false;
                    oldMeshHolders[i] = tmp;

                    break;
                }
            }


            //Debug.Log("lowest dist: " + minDist);
        }

        return equalMeshIndex;
    }
}

/*
//get count again to make sure its empty (cuz it FINALLY IS)
ComputeBuffer.CopyCount(trianglesBuffer, triCountBuffer, 0);
triCountBuffer.GetData(triCountArray);
numTris = triCountArray[0];
Debug.Log("Bytes2: " + ((numTris * sizeof(float) * 3 * 3)));
*/

//trianglesBuffer.SetData(new Triangle[] { });
