using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class UnityExtensions
{
    public static bool IsInLayerMask(this LayerMask lasyerMask, int layer)
    {
        return lasyerMask == (lasyerMask | (1 << layer));
    }
    
    public static int LayerMaskToLayer(this LayerMask layerMask)
    {
        int layerNumber = 0;
        int layer = layerMask.value;
        while (layer > 0)
        {
            layer = layer >> 1;
            layerNumber++;
        }

        return layerNumber - 1;
    }

    public static float GetMaxAxisSize(this Bounds bounds)
    {
        return Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    }

    public static void DestroyChildren(this Transform holder, List<GameObject> exludeList = null)
    {
        foreach (Transform child in holder)
        {
            if (exludeList != null && exludeList.Contains(child.gameObject)) continue;
            UnityEngine.Object.Destroy(child.gameObject);
        }

        Canvas.ForceUpdateCanvases();
    }

    public static void DestroyChildrenImmediate(this Transform holder, List<GameObject> exludeList = null)
    {
        var list = holder.Cast<Transform>().ToList();
        foreach (Transform child in list)
        {
            if (exludeList != null && exludeList.Contains(child.gameObject)) continue;
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    public static Bounds GetBounds(this Transform holder, string childName)
    {
        Transform child = holder.Find(childName);
        if (child != null)
        {
            return GetBounds(child);
        }

        return GetBounds(holder);
    }

    public static void SetAlpha(this Image image, float alpha)
    {
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }

    public static void SetLayerRecursively(this GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            child.gameObject.SetLayerRecursively(layer);
        }
    }

    public static float Height(this Bounds bounds)
    {
        return Mathf.Abs(bounds.size.y);
    }

    public static float Width(this Bounds bounds)
    {
        return Mathf.Abs(bounds.size.x);
    }

    public static Bounds GetCalculatedBounds(this Transform holder)
    {
        var bounds = new Bounds();
        Collider[] colliders = holder.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            foreach (var collider in colliders)
            {
                bounds.Encapsulate(collider.bounds);
            }
        }
        else
        {
            Renderer[] renderers = holder.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return bounds;
    }

    public static Bounds GetBounds(this Transform holder)
    {
        Collider collider = holder.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }

        Collider2D col = holder.transform.GetComponent<Collider2D>();
        if (col != null)
        {
            return col.bounds;
        }

        Renderer renderer = holder.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        return new Bounds(Vector3.one * 0.5f, Vector3.one);
    }
}