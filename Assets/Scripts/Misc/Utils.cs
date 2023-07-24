using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static GameObject instantiate(GameObject prefab, Vector3 position, Transform parent, Color color, string layer = "") {
        GameObject go = instantiate(prefab,position, parent, layer);
        go.GetComponent<Renderer>().material.color = color;
        return go;
    }

    public static GameObject instantiate(GameObject prefab, Vector3 position, Transform parent, string layer = "") {
        GameObject go = Instantiate(prefab, position, Quaternion.identity);
        go.transform.parent = parent;
        if(layer != "") go.layer = LayerMask.NameToLayer(layer);
        return go;
    }

    // taken and adapted from old unity code: https://github.com/Unity-Technologies/Graphics/pull/2287/
    public static void DrawSphere(Vector4 pos, float radius, Color color, float durationInSeconds = 0)
    {
        Vector4[] v = MakeUnitSphere(16);;
        int len = v.Length / 3;
        for (int i = 0; i < len; i++)
        {
            var sX = pos + radius * v[0 * len + i];
            var eX = pos + radius * v[0 * len + (i + 1) % len];
            var sY = pos + radius * v[1 * len + i];
            var eY = pos + radius * v[1 * len + (i + 1) % len];
            var sZ = pos + radius * v[2 * len + i];
            var eZ = pos + radius * v[2 * len + (i + 1) % len];
            if(durationInSeconds > 0) {
                Debug.DrawLine(sX, eX, color, durationInSeconds);
                Debug.DrawLine(sY, eY, color, durationInSeconds);
                Debug.DrawLine(sZ, eZ, color, durationInSeconds);
            } else {
                Debug.DrawLine(sX, eX, color);
                Debug.DrawLine(sY, eY, color);
                Debug.DrawLine(sZ, eZ, color);
            }
        }
    }
    
    private static Vector4[] MakeUnitSphere(int len)
    {
        Debug.Assert(len > 2);
        var v = new Vector4[len * 3];
        for (int i = 0; i < len; i++)
        {
            var f = i / (float)len;
            float c = Mathf.Cos(f * (float)(Mathf.PI * 2.0));
            float s = Mathf.Sin(f * (float)(Mathf.PI * 2.0));
            v[0 * len + i] = new Vector4(c, s, 0, 1);
            v[1 * len + i] = new Vector4(0, c, s, 1);
            v[2 * len + i] = new Vector4(s, 0, c, 1);
        }
        return v;
    }
}
