using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GoogleARCore;

public class PlaneGenerator : MonoBehaviour {
    private static PlaneGenerator _instance = null;
    public static PlaneGenerator Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(PlaneGenerator)) as PlaneGenerator;
                if (!_instance)
                {
                    Debug.Log("ERROR : NO PlaneGenerator");
                }
            }
            return _instance;
        }
    }

    // 생성될 plane 프리팹
    public GameObject planePrefab;

    // 현재 프레임에 있는 모든 Plane을 저장하는 List
    private List<DetectedPlane> newPlanes = new List<DetectedPlane>();
    
	void Update ()
    {
        if (Session.Status != SessionStatus.Tracking)
            return;

        Session.GetTrackables<DetectedPlane>(newPlanes, TrackableQueryFilter.New);
        for(int i = 0; i < newPlanes.Count; i++)
        {
            GameObject planeObject = Instantiate(planePrefab, Vector3.zero, 
                                                Quaternion.identity, transform);
            // PlaneVisualizer 컴포넌트를 초기화하여 Plane 가시화
            planeObject.GetComponent<PlaneVisualizer>().Initialize(newPlanes[i]);

        }
	}
}
