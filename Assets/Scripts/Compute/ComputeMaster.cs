using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeMaster : MonoBehaviour
{
    public ComputeShader marchingShader;

    public Transform playerMeshTransform;
    public Gun gun;
    public float surfaceLevel;

    [Range(1, 1)]
    public float worldZoom = 1f;

    public int numPointsPerAxis;
    public float offsetX = 0f;
    public float offsetY = 0f;
    public float offsetZ = 0f;
    public float perlinZoom = 2f;
    //public int worldScale = 1;

    public int sphereRadius = 10;

    public Color lightColor;
    public Color darkColor;

    public GameObject meshHolder;

    private Vector3 lastBeenSector = Vector3.zero;
    private GameObject s;
    private Triangle[] tris;    
    private float[,,] points;
    private int numVoxelsPerAxis;
    private bool firstFrame;
    private int sphereCount = 0;
    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer spheresBuffer;
    private ComputeBuffer numTrisBuffer;

    private List<Sphere> spheres;

    private GameObject curMeshHolder;
    private Vector3 offsetVec;
    private Vector3 minCornerPos;
    private Vector3 currentSector;
    private int maxTriangleCount;

    private List<Chunk> meshHolders;

    private Camera mainCamera;

    private float startingTime = 0;
    private float timeBeforeShader = 0;
    private float timeAfterShader  = 0;
    private float timeAfterGettingShaderData = 0;
    private float timeAfterMeshAssembly = 0;

    struct Triangle {
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

    struct Chunk {
        public Vector3 position;
        public GameObject meshHolder;
        public bool used;
        public bool destroy;
        public Vector3[] vertices;
        public int[] triangles;
    }

    struct Sphere {
        public float x;
        public float y;
        public float z;
        public int surfaceValue;
        public int index;
    }

    void Start()
    {
        mainCamera = Camera.main;

        int numVoxels = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        maxTriangleCount = numVoxels * 50;
        trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        Debug.Log(maxTriangleCount * 36);

        spheres = new List<Sphere>();

        curMeshHolder = Instantiate(meshHolder, Vector3.zero, Quaternion.identity, transform);

        curMeshHolder.GetComponent<MeshFilter>().sharedMesh = new Mesh();
 
        currentSector = Vector3.zero;
        meshHolders = new List<Chunk>();
        for (int x = -2; x <= 2; x++) {
            for (int y = -2; y <= 2; y++) {
                for (int z = -2; z <= 2; z++) {
                    GameObject tmpMeshHolder = Instantiate(meshHolder, Vector3.zero, Quaternion.identity, transform);
                    Mesh curMesh = new Mesh();
                    tmpMeshHolder.GetComponent<MeshFilter>().sharedMesh = curMesh;
                    tmpMeshHolder.GetComponent<MeshCollider>().sharedMesh = curMesh;
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
        startingTime = Time.realtimeSinceStartup;

        Vector3 playerPosition = playerMeshTransform.position;
        Vector3 nextSector = new Vector3(playerPosition.x - mod(playerPosition.x, numPointsPerAxis) - numPointsPerAxis,
                                         playerPosition.y - mod(playerPosition.y, numPointsPerAxis) - numPointsPerAxis,
                                         playerPosition.z - mod(playerPosition.z, numPointsPerAxis) - numPointsPerAxis);

        //spheres done by pressing space (to be removed)
        if (Input.GetKeyDown(KeyCode.Space)) { 
            createSphere(playerMeshTransform.position, true, true);
        }
        if(firstFrame) {
            clearMeshes();
        }
        bool sectorChanged = firstFrame || Vector3.Distance(currentSector, nextSector) >= 0.2f;

        if (sectorChanged) march(nextSector);
    }

    private void march(Vector3 nextSector) {

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
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                                
                    Vector3 targetCoordinate = currentSector + new Vector3(
                        numPointsPerAxis * x,
                        numPointsPerAxis * y,
                        numPointsPerAxis * z);// / worldZoom;

                    int meshIndex = getMeshIndex(oldChunks, targetCoordinate);

                    if (meshIndex > -1) {
                        skippedCount++;
                        meshHolders.Add(oldChunks[meshIndex]);
                    } else {
                        numTrisBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
                        numTrisBuffer.SetCounterValue(0);

                        //Run shader
                        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / 8.0f);
                        SetShaderParameters(targetCoordinate);
                        timeBeforeShader = Time.realtimeSinceStartup;
                        marchingShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
                        timeAfterShader = Time.realtimeSinceStartup;

                        //Get shader data
                        int trisCount = getNumberOfTris();
                        tris = new Triangle[trisCount];
                        trianglesBuffer.GetData(tris, 0, 0, trisCount);

                        timeAfterGettingShaderData = Time.realtimeSinceStartup;

                        // fill mesh 
                        var vertices = new Vector3[trisCount * 3];
                        var triangles = new int[trisCount * 3];

                        for (int i = 0; i < trisCount; i++)
                        {
                            vertices[3 * i + 0] = tris[i].a;
                            vertices[3 * i + 1] = tris[i].b;
                            vertices[3 * i + 2] = tris[i].c;
                            triangles[3 * i + 0] = 3 * i + 0;
                            triangles[3 * i + 1] = 3 * i + 1;
                            triangles[3 * i + 2] = 3 * i + 2;
                        }
                        
                        Mesh mesh = createMesh(vertices, triangles);
                        Chunk chunk = createChunk(mesh, targetCoordinate);
                        meshHolders.Add(chunk);

                        timeAfterMeshAssembly = Time.realtimeSinceStartup;

                        //Cleanup
                        trianglesBuffer.Release();
                        trianglesBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
                        trianglesBuffer.SetCounterValue(0);
                        if(spheresBuffer != null) {
                            spheresBuffer.Release();
                        }
                        numTrisBuffer.Dispose();
                    }
                    meshHolderIndex++;
                }
            }
        }
        destroyUnusedMeshes(oldChunks);
        
        //disabled
        logPerformance(false, startingTime, timeBeforeShader, timeAfterShader, timeAfterGettingShaderData, timeAfterMeshAssembly);
    }

    private void SetShaderParameters(Vector3 minCorner) {
        marchingShader.SetBuffer(0, "triangles", trianglesBuffer);
        marchingShader.SetBuffer(0, "numTris", numTrisBuffer);

        // Booleans are 4 bytes in HLSL so sizeof(int) is used instead.
        spheresBuffer = new ComputeBuffer(spheres.Count + 1, sizeof(float)*3 + sizeof(int) + sizeof(int), ComputeBufferType.Structured);
        spheresBuffer.SetData((Sphere[]) spheres.ToArray());

        marchingShader.SetBuffer(0, "spheres", spheresBuffer);

        marchingShader.SetInt("sphereCount", spheres.Count);

        marchingShader.SetInt("sphereRadius", sphereRadius);
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

    private Mesh createMesh(Vector3[] vertices, int[] triangles) {
        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        //newMesh.RecalculateBounds();
        //newMesh.RecalculateTangents();
        //color mesh
        mainCamera.backgroundColor = darkColor;
        Color[] colors = new Color[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            colors[i] = Color.Lerp(lightColor, darkColor, Mathf.PerlinNoise(vertices[i][0]/5f, vertices[i][1]/5f)*2f - 0.5f);
        mesh.colors = colors;

        return mesh;
    }

    private Chunk createChunk(Mesh mesh, Vector3 pos) {
        Chunk chunk = new Chunk();
        chunk.destroy = false;
        chunk.used = true;
        chunk.position = pos;
        chunk.meshHolder = Instantiate(meshHolder, Vector3.zero, Quaternion.identity, transform);

        MeshFilter meshFilter = chunk.meshHolder.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        MeshCollider meshCollider = chunk.meshHolder.GetComponent<MeshCollider>();

        // force update
        meshCollider.enabled = false;
        meshCollider.enabled = true;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        meshCollider.enabled = false;
        meshCollider.enabled = true;

        return chunk;
    }

    //modulo that works for negative numbers
    private float mod(float k, float n) {
        return ((k %= n) < 0) ? k + n : k;
    }

    private int getMeshIndex(List<Chunk> meshList, Vector3 targetCoordinate) {
        int equalMeshIndex = -1;
        for (int i = 0; i < meshList.Count; i++)
        {
            if (meshList[i].used)
            {
                var dist = Vector3.Distance(meshList[i].position, targetCoordinate);

                if (dist < 0.1f && meshList[i].used)
                {
                    //save index
                    equalMeshIndex = i;

                    //set destroy to false
                    Chunk tmp = meshList[i];
                    tmp.destroy = false;
                    meshList[i] = tmp;

                    break;
                }
            }
        }

        return equalMeshIndex;
    }

    private void destroyUnusedMeshes(List<Chunk> chunks) {
        for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].destroy)
                {
                    Destroy(chunks[i].meshHolder);
                }
            }
    }
    
    public void createSphere(Vector3 sphereCenter, bool isAirSphere, bool clearTerrain) {
        sphereCount += 1;
        Sphere sphere = new Sphere();
        Debug.Log("offset: " + offsetVec);
        sphereCenter = sphereCenter - new Vector3(10, 10, 10);
        sphere.x = sphereCenter.x;
        sphere.y = sphereCenter.y;
        sphere.z = sphereCenter.z;
        sphere.surfaceValue = isAirSphere ? 1 : -1;
        sphere.index = spheres.Count;
        spheres.Add(sphere);

        if(clearTerrain) firstFrame = true;;
    }

    private void clearMeshes() {
        for (int i = 0; i < meshHolders.Count; i++)
        {
            Destroy(meshHolders[i].meshHolder);
        }
        meshHolders = new List<Chunk>();
    }

    private int getNumberOfTris() {
        ComputeBuffer tmpBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        int[] triCountArray = new int[1] {0};
        ComputeBuffer.CopyCount(numTrisBuffer, tmpBuffer, 0);
        tmpBuffer.GetData(triCountArray);
        tmpBuffer.Dispose();

        return triCountArray[0];
    }
    
    private void logPerformance(
        bool doLog, 
        float startingTime, 
        float timeBeforeShader, 
        float timeAfterShader, 
        float timeAfterGettingShaderData, 
        float endingTime) 
    {
        if(!doLog) return;
        
        Debug.Log("-------------------");
        Debug.Log("Performance measures:");
        Debug.Log("Time in update overall:" + (endingTime - startingTime));
        Debug.Log("Time before shader: " + (timeBeforeShader - startingTime));
        Debug.Log("Time in shader: " + (timeAfterShader - timeBeforeShader));
        Debug.Log("Time getting shader data: " + (timeAfterGettingShaderData - timeAfterShader));
        Debug.Log("Time in mesh assembly: " + (endingTime - timeAfterGettingShaderData));
        Debug.Log("-------------------");
        Debug.Log("");
    }
}
