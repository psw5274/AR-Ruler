using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using GoogleARCore;

public class UIManager : MonoBehaviour
{
    // 싱글톤 패턴
    private static UIManager _instance = null;
    public static UIManager Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(UIManager)) as UIManager;
                if (!_instance)
                {
                    Debug.Log("ERROR : NO UIManager");
                }
            }
            return _instance;
        }
    }
    // 싱글톤 패턴
    
    [SerializeField]
    private GameObject textPrefab;
    
    private List<GameObject> textList = new List<GameObject>();

    // Button의 OnClick 이벤트와 연동하여 모드를 변동시켜줌
    public void ChangeDistMode() { ARCoreController.Instance.mode = 1; }
    public void ChangeAccDistMode() { ARCoreController.Instance.mode = 2; }
    public void ChangeAngleMode() { ARCoreController.Instance.mode = 3; }

    // Anchor UI 초기화
    public void ClearUI()
    {
        foreach (var text in textList)
            Destroy(text);
        textList.Clear();

        ARCoreController.Instance.ClearAnchorList();

        Debug.Log("CLEAR UI!");
    }

    // 두 Anchor를 잇고 거리 표시
    public void DrawDistanceBetweenObject(GameObject a, GameObject b, float distance)
    {
        // cm단위로 두 Anchor 사이의 길이를 Text로 표시
        Vector3 targetPosition = (a.transform.position + b.transform.position) / 2;
        GameObject text = Instantiate(textPrefab, targetPosition, Quaternion.identity);
        text.GetComponent<TextMesh>().text = (((int)(distance*100))/100f).ToString() + "cm";
        textList.Add(text);

        // LineRenedering을 통해 두 Anchor의 오브젝트를 연결
        LineRenderer line;
        line = a.gameObject.AddComponent<LineRenderer>();
        line.SetWidth(0.01f, 0.01f);
        line.SetVertexCount(2);
        line.SetPosition(0, a.transform.position);
        line.SetPosition(1, b.transform.position);

    }

    // 각이 되는 Anchor에 각 표시
    public void DrawAngleBetweenAnchor(GameObject a, float angle)
    {
        GameObject text = Instantiate(textPrefab, a.transform.position, Quaternion.identity);
        text.GetComponent<TextMesh>().text = (((int)(angle * 100)) / 100f).ToString() + "'";
        textList.Add(text);
    }
}
