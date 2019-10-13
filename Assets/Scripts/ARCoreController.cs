using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GoogleARCore;

#if UNITY_EDITOR
    using Input = GoogleARCore.InstantPreviewInput;
#endif

public class ARCoreController : MonoBehaviour
{
    private static ARCoreController _instance = null;
    public static ARCoreController Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(ARCoreController)) as ARCoreController;
                if (!_instance)
                {
                    Debug.Log("ERROR : NO ARCoreController");
                }
            }
            return _instance;
        }
    }


    [SerializeField]
    private Camera firstPersonCamera;

    [SerializeField]
    private GameObject anchorModelPrefab;

    private List<GameObject> distanceAnchorList = new List<GameObject>();
    private List<GameObject> accDistanceAnchorList = new List<GameObject>();
    private List<GameObject> angleAnchorList = new List<GameObject>();
    private Anchor prevAnchor = null;

    // 1 : distance, 2 : AccDistance, 3 : Angle
    public int mode = 1;
    private int numAnchor = 0;
    private float accDistance = 0;

    public void ClearAnchorList()
    {
        foreach (var anchor in distanceAnchorList)
            Destroy(anchor);
        distanceAnchorList.Clear();

        foreach (var anchor in accDistanceAnchorList)
            Destroy(anchor);
        accDistanceAnchorList.Clear();
        accDistance = 0;

        foreach (var anchor in angleAnchorList)
            Destroy(anchor);
        angleAnchorList.Clear();
    }
    

    void Update()
    {
        UpdateApplicationLifecycle();
        RenderLine();

        // 터치가 없다면 아래의 Update 명령을 건너 뜀
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            return;

        TouchScreen(touch);

    }
    
    // 안드로이드 어플리케이션 관리 함수
    private void UpdateApplicationLifecycle()
    {
        // 뒤로가기 클릭시 앱 종료
        if (Input.GetKey(KeyCode.Escape))
            QuitApplication();

        // 모션 트래킹 중 슬립 방지
        if (Session.Status != SessionStatus.Tracking)
        {
            const int lostTrackingSleepTimeout = 15;
            Screen.sleepTimeout = lostTrackingSleepTimeout;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        // 에러, 안드로이드 권한 없을시 종료
        if (Session.Status == SessionStatus.ErrorPermissionNotGranted || Session.Status.IsError())
        {
            ShowAndroidToastMessage("Error with ARCore app.");
            Invoke("QuitApplication", 0.5f);
        }
    }
    
    // 디버깅을 위한 안드로이드 토스트 메세지를 작성한다
    private void ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                    message, 0);
                toastObject.Call("show");
            }));
        }
    }

    private void TouchScreen(Touch touch)
    {
        // GUI 터치와 플레이 화면 터치의 충돌 방지
        if(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;
        TrackableHit hit;
        TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
            TrackableHitFlags.FeaturePointWithSurfaceNormal;

        // ARCORE에 트래킹된 오브젝트에 Raycast하여 터치한 오브젝트가 Plane인지 판별
        if (Frame.Raycast(touch.position.x, touch.position.y, raycastFilter, out hit))
        {
            // Plane 터치 오작동 예외처리
            if ((hit.Trackable is DetectedPlane) &&
                Vector3.Dot(firstPersonCamera.transform.position - hit.Pose.position,
                    hit.Pose.rotation * Vector3.up) < 0)
            {
                Debug.Log("Hit at back of the current DetectedPlane");
            }

            // Plane 터치 시 Anchor 생성
            else
            {
                float distance;
                int cnt;
                List<GameObject> list;
                // 실세계의 좌표를 저장하는 'Anchor' 오브젝트와 이를 가시화하는 모델 생성
                Anchor anchor = hit.Trackable.CreateAnchor(hit.Pose);
                GameObject anchorModel = Instantiate(anchorModelPrefab,
                                        hit.Pose.position, hit.Pose.rotation);
                anchorModel.transform.parent = anchor.transform;

                switch (mode)
                {
                    case 1: // Distance mode
                        list = distanceAnchorList;
                        list.Add(anchor.gameObject);
                        cnt = list.Count;

                        // 저장된 Anchor가 짝수이면 거리 계산 및 표시
                        if (cnt % 2 == 0)
                        {
                            distance = GetDistanceByObject(list[cnt-2], list[cnt-1]);
                            UIManager.Instance.DrawDistanceBetweenObject(list[cnt - 2], list[cnt - 1], distance);
                            Debug.Log(distance + "cm");
                        }
                        break;

                    case 2: // AccDistance mode
                        list = accDistanceAnchorList;
                        list.Add(anchor.gameObject);
                        cnt = list.Count;

                        // 저장된 Anchor가 2개 이상이면 거리 계산 및 표시
                        if (cnt > 1)
                        {
                            accDistance += GetDistanceByObject(list[cnt - 2], list[cnt - 1]);
                            UIManager.Instance.DrawDistanceBetweenObject(list[cnt - 2], list[cnt - 1], accDistance);
                            Debug.Log(accDistance + "cm");
                        }
                        break;

                    case 3: // Angle mode
                        list = angleAnchorList;
                        list.Add(anchor.gameObject);
                        cnt = list.Count;

                        // 3개의 Anchor로 각을 형성
                        if (cnt % 3 == 0)
                        {
                            float angle = GetAngleByObject(list[cnt - 3], list[cnt - 2], list[cnt - 1]);
                            UIManager.Instance.DrawAngleBetweenAnchor(list[cnt - 2], angle);
                            Debug.Log(angle + "'");

                            // Anchor가 3개 모이면 이들을 LineRendering으로 연결함
                            for (int i = -3; i < -1; i++)
                            {
                                LineRenderer line;
                                line = list[cnt + i].gameObject.AddComponent<LineRenderer>();
                                line.SetWidth(0.01f, 0.01f);
                                line.SetVertexCount(2);
                                line.SetPosition(0, list[cnt + i].transform.position);
                                line.SetPosition(1, list[cnt + i+1].transform.position);
                            }

                        }
                        break;
                }
            }
        }
    }

    // Tracked Pose Driver에서 Relative Transform을 활성할 경우
    // Anchor는 실세계의 좌표를 어플리케이션에서 상대좌표로 치환한다
    private float GetDistanceByObject(GameObject currentAnchor, GameObject prevAnchor)
    {
        return (currentAnchor.transform.position - prevAnchor.transform.position).magnitude*100;
    }

    private float GetAngleByObject(GameObject a, GameObject b, GameObject c)
    {
        float angle = Vector3. Angle(a.transform.position-b.transform.position,
                                        c.transform.position-b.transform.position);
        return angle;
    }

    // 추후 리팩토링 필요
    private void RenderLine()
    {
        var list = distanceAnchorList;
        for (int i = 0; i < (list.Count % 2 == 0? list.Count: list.Count-1); i+=2)
        {
            list[i].GetComponent<LineRenderer>().SetPosition(0, list[i].transform.position);
            list[i].GetComponent<LineRenderer>().SetPosition(1, list[i+1].transform.position);
        }

        list = accDistanceAnchorList;
        for (int i = 0; i < list.Count-1; i++)
        {
            list[i].GetComponent<LineRenderer>().SetPosition(0, list[i].transform.position);
            list[i].GetComponent<LineRenderer>().SetPosition(1, list[i+1].transform.position);
        }
        
        list = angleAnchorList;
        for(int i = 0; i*3+2 < list.Count; i++)
        {
            list[i*3].GetComponent<LineRenderer>().SetPosition(0, list[i*3].transform.position);
            list[i*3].GetComponent<LineRenderer>().SetPosition(1, list[i*3+1].transform.position);

            list[i*3+1].GetComponent<LineRenderer>().SetPosition(0, list[i*3+1].transform.position);
            list[i*3+1].GetComponent<LineRenderer>().SetPosition(1, list[i*3+2].transform.position);
        }

    }

    // 애플리케이션 종료
    public void QuitApplication()
    {
        Application.Quit();
    }
}
