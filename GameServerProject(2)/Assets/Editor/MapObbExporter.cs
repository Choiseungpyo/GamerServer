using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapObbExporter
{
    private const string LayerName = "MapCollision";

    [MenuItem("Tools/Export/Map OBB Binary")]
    public static void Export()
    {
        int layer = LayerMask.NameToLayer(LayerName);
        if (layer < 0)
        {
            EditorUtility.DisplayDialog("Error", "Layer not found: " + LayerName, "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel(
            "Save map_obb.bin",
            Application.dataPath,
            "map_obb",
            "bin"
        );

        if (string.IsNullOrEmpty(path))
            return;

        BoxCollider[] all = UnityEngine.Object.FindObjectsOfType<BoxCollider>(true);

        // file format
        // int32 count
        // center(float3), half(float3), rotation(float4) repeated
        List<(Vector3 c, Vector3 half, Quaternion q)> list = new List<(Vector3, Vector3, Quaternion)>(all.Length);

        for (int i = 0; i < all.Length; i++)
        {
            BoxCollider bc = all[i];
            if (bc == null) continue;
            if (!bc.enabled) continue;
            if (bc.isTrigger) continue;

            GameObject go = bc.gameObject;
            if (go == null) continue;
            if (!go.activeInHierarchy) continue;
            if (go.layer != layer) continue;

            Transform t = bc.transform;

            Vector3 worldCenter = t.TransformPoint(bc.center);

            Vector3 ls = t.lossyScale;
            Vector3 scaledSize = new Vector3(
                bc.size.x * Mathf.Abs(ls.x),
                bc.size.y * Mathf.Abs(ls.y),
                bc.size.z * Mathf.Abs(ls.z)
            );
            Vector3 half = scaledSize * 0.5f;

            Quaternion rot = t.rotation;

            if (half.x <= 0.00001f || half.y <= 0.00001f || half.z <= 0.00001f)
                continue;

            list.Add((worldCenter, half, rot));
        }

        try
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    bw.Write(e.c.x); bw.Write(e.c.y); bw.Write(e.c.z);
                    bw.Write(e.half.x); bw.Write(e.half.y); bw.Write(e.half.z);
                    bw.Write(e.q.x); bw.Write(e.q.y); bw.Write(e.q.z); bw.Write(e.q.w);
                }
            }

            EditorUtility.DisplayDialog("Done", "Exported count: " + list.Count + "\nFile: " + path, "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }
}
