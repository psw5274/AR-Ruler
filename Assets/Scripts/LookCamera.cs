using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookCamera : MonoBehaviour
{
    Vector3 toMainCamera;
    Vector3 defaultScale = new Vector3(0.02f, 0.02f, 0.02f);

    void Update ()
    {
        toMainCamera = transform.position - Camera.main.transform.position;
        // 카메라를 바라봄
        transform.rotation = Quaternion.LookRotation(toMainCamera);

        // 카메라 거리에 비례한 글자 크기
        transform.localScale = toMainCamera.magnitude * defaultScale;
    }
}
