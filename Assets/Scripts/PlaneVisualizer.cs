using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

// GoogleARCore.Examples.Common.DetectedPlaneVisualizer

public class PlaneVisualizer : MonoBehaviour
{
    private static int s_PlaneCount = 0;


    private readonly Color[] planeColors = new Color[]
    {
        new Color(1.0f, 1.0f, 1.0f),
        new Color(0.956f, 0.262f, 0.211f),
        new Color(0.913f, 0.117f, 0.388f),
        new Color(0.611f, 0.152f, 0.654f),
        new Color(0.403f, 0.227f, 0.717f),
        new Color(0.247f, 0.317f, 0.709f),
        new Color(0.129f, 0.588f, 0.952f),
        new Color(0.011f, 0.662f, 0.956f),
        new Color(0f, 0.737f, 0.831f),
        new Color(0f, 0.588f, 0.533f),
        new Color(0.298f, 0.686f, 0.313f),
        new Color(0.545f, 0.764f, 0.290f),
        new Color(0.803f, 0.862f, 0.223f),
        new Color(1.0f, 0.921f, 0.231f),
        new Color(1.0f, 0.756f, 0.027f)
    };

    private DetectedPlane detectedPlane;

    private Vector3 planeCenter = new Vector3();

    private List<Vector3> previousFrameMeshVertices = new List<Vector3>();
    private List<Vector3> meshVertices = new List<Vector3>();
    private List<Color> meshColors = new List<Color>();
    private List<int> meshIndices = new List<int>();
    private Mesh mesh;
    private MeshRenderer meshRenderer;

    void Awake ()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        meshRenderer = GetComponent<UnityEngine.MeshRenderer>();
	}
	
	void Update ()
    {
        if (detectedPlane == null)
            return;
        else if (detectedPlane.SubsumedBy != null)
        {
            Destroy(gameObject);
            return;
        }
        else if (detectedPlane.TrackingState != TrackingState.Tracking)
        {
            meshRenderer.enabled = false;
            return;
        }
        meshRenderer.enabled = true;
        _UpdateMeshIfNeeded();
    }

    public void Initialize(DetectedPlane plane)
    {
        detectedPlane = plane;
        meshRenderer.material.SetColor("_GridColor", planeColors[s_PlaneCount++ % planeColors.Length]);
        meshRenderer.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
    }

    private void _UpdateMeshIfNeeded()
    {
        detectedPlane.GetBoundaryPolygon(meshVertices);

        if (_AreVerticesListsEqual(previousFrameMeshVertices, meshVertices))
        {
            return;
        }

        previousFrameMeshVertices.Clear();
        previousFrameMeshVertices.AddRange(meshVertices);

        planeCenter = detectedPlane.CenterPose.position;

        Vector3 planeNormal = detectedPlane.CenterPose.rotation * Vector3.up;

        meshRenderer.material.SetVector("_PlaneNormal", planeNormal);

        int planePolygonCount = meshVertices.Count;
        
        meshColors.Clear();
        
        for (int i = 0; i < planePolygonCount; ++i)
        {
            meshColors.Add(Color.clear);
        }

        const float featherLength = 0.2f;

        const float featherScale = 0.2f;

        for (int i = 0; i < planePolygonCount; ++i)
        {
            Vector3 v = meshVertices[i];

            Vector3 d = v - planeCenter;

            float scale = 1.0f - Mathf.Min(featherLength / d.magnitude, featherScale);
            meshVertices.Add((scale * d) + planeCenter);

            meshColors.Add(Color.white);
        }

        meshIndices.Clear();
        int firstOuterVertex = 0;
        int firstInnerVertex = planePolygonCount;

        for (int i = 0; i < planePolygonCount - 2; ++i)
        {
            meshIndices.Add(firstInnerVertex);
            meshIndices.Add(firstInnerVertex + i + 1);
            meshIndices.Add(firstInnerVertex + i + 2);
        }

        for (int i = 0; i < planePolygonCount; ++i)
        {
            int outerVertex1 = firstOuterVertex + i;
            int outerVertex2 = firstOuterVertex + ((i + 1) % planePolygonCount);
            int innerVertex1 = firstInnerVertex + i;
            int innerVertex2 = firstInnerVertex + ((i + 1) % planePolygonCount);

            meshIndices.Add(outerVertex1);
            meshIndices.Add(outerVertex2);
            meshIndices.Add(innerVertex1);

            meshIndices.Add(innerVertex1);
            meshIndices.Add(outerVertex2);
            meshIndices.Add(innerVertex2);
        }

        mesh.Clear();
        mesh.SetVertices(meshVertices);
        mesh.SetIndices(meshIndices.ToArray(), MeshTopology.Triangles, 0);
        mesh.SetColors(meshColors);
    }
    private bool _AreVerticesListsEqual(List<Vector3> firstList, List<Vector3> secondList)
    {
        if (firstList.Count != secondList.Count)
        {
            return false;
        }

        for (int i = 0; i < firstList.Count; i++)
        {
            if (firstList[i] != secondList[i])
            {
                return false;
            }
        }

        return true;
    }
}
