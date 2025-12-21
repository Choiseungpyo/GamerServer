using UnityEngine;

public class MapBoundsPrinter : MonoBehaviour
{
    [ContextMenu("Print Map Bounds")]
    private void PrintBounds()
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0)
        {
            Debug.Log("No Renderer found under Map");
            return;
        }

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);

        Debug.Log("MapBounds center=" + b.center + " size=" + b.size);
        Debug.Log("Use server worldW=" + b.size.x + " worldD=" + b.size.z);
    }
}