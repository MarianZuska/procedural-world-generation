using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratorHelper : MonoBehaviour
{

    public bool doLog = false;
    
    public Vector3[,,] boundingBox;
    
    private ComputeMaster computeMaster;
    private List<GameObject> spheres = new List<GameObject>();

    void Start()
    {
        computeMaster = GetComponent<ComputeMaster>();
        boundingBox = new Vector3[2,2,2];
    }

    void Update()
    {
        setBoundingBox();
        if(doLog) showBoundingBox();
    }

    public bool isInsideBoundingBox(Vector3 pos) {
        for(int x = 0; x <= 1; x++) {
            for(int y = 0; y <= 1; y++) {
                for(int z = 0; z <= 1; z++) {
                    bool bx = (x==1);
                    bool by = (y==1);
                    bool bz = (z==1);

                    Vector3 corner = boundingBox[x, y, z];

                    if( bx && pos.x > corner.x || 
                       !bx && pos.x < corner.x || 
                        by && pos.y > corner.y || 
                       !by && pos.y < corner.y || 
                        bz && pos.z > corner.z || 
                       !bz && pos.z < corner.z) return false;
                }
            }
        }
        return true;

    }

    private void setBoundingBox()
    {
        List<ComputeMaster.Chunk> chunks = computeMaster.chunks;
        if(chunks.Count == 0) {
            Debug.LogError("No MeshHolders found!");
            return;
        }

        // initialize bounding box
        Vector3 defaultPos = chunks[0].position;
        for(int x = 0; x <= 1; x++) {
            for(int y = 0; y <= 1; y++) {
                for(int z = 0; z <= 1; z++) {
                    boundingBox[x,y,z] = defaultPos;
                }
            }
        }

        for(int i = 0; i<chunks.Count; i++) {
            Vector3 targetCoordinate = chunks[i].position;

            // iterate through all bounding box corners and attempt update
            // TODO: improve
            for(int x = 0; x <= 1; x++) {
                for(int y = 0; y <= 1; y++) {
                    for(int z = 0; z <= 1; z++) {
                        bool replace = true;

                        bool bx = (x==1);
                        bool by = (y==1);
                        bool bz = (z==1);

                        Vector3 coord = targetCoordinate + (new Vector3(x,y,z) * computeMaster.worldZoom * computeMaster.numPointsPerAxis);

                        // GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        // sphere.transform.position = coord + new Vector3(offsetX, offsetY, offsetZ);
                        // sphere.transform.localScale = new Vector3(50f, 50f, 50f);

                        if(bx && coord.x < boundingBox[x,y,z].x) replace = false;
                        if(!bx && coord.x > boundingBox[x,y,z].x) replace = false;
                        if(by && coord.y < boundingBox[x,y,z].y) replace = false;
                        if(!by && coord.y > boundingBox[x,y,z].y) replace = false;
                        if(bz && coord.z < boundingBox[x,y,z].z) replace = false;
                        if(!bz && coord.z > boundingBox[x,y,z].z) replace = false;
                        
                        if(replace) boundingBox[x,y,z] = coord;
                    }
                }
            }
        }
    }

    private void showBoundingBox() {
        for(int i = 0; i<spheres.Count; i++) {
            Destroy(spheres[i]);
        }
        spheres = new List<GameObject>();

        for(int x = 0; x <= 1; x++) {
            for(int y = 0; y <= 1; y++) {
                for(int z = 0; z <= 1; z++) {
                    
                    Debug.Log(x + " " + y + " " + z + ": " + boundingBox[x,y,z]);
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.position = boundingBox[x,y,z] + new Vector3(computeMaster.offsetX, computeMaster.offsetY, computeMaster.offsetZ);
                    sphere.transform.localScale = new Vector3(100f, 100f, 100f);
                    spheres.Add(sphere);
                }
            }
        }
    }
}
