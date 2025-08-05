using UnityEngine;
using System.Collections.Generic;

public class BallTrailGhost : MonoBehaviour
{
    public GameObject ghostPrefab;
    public int ghostCount = 5;
    public float recordInterval = 0.08f; // 잔상 기록 간격(초)

    private struct GhostInfo
    {
        public GameObject obj;
        public Material mat;
    }

    private List<GhostInfo> ghostPool;
    private Queue<Vector3> positionHistory;
    private Queue<Quaternion> rotationHistory;
    private float recordTimer = 0f;

    void Start()
    {
        ghostPool = new List<GhostInfo>(ghostCount);
        positionHistory = new Queue<Vector3>(ghostCount);
        rotationHistory = new Queue<Quaternion>(ghostCount);

        for (int i = 0; i < ghostCount; i++)
        {
            var go = Instantiate(ghostPrefab, transform.position, transform.rotation);
            go.SetActive(false);
            var renderer = go.GetComponent<MeshRenderer>();
            Material mat = renderer != null ? renderer.material : null;
            ghostPool.Add(new GhostInfo { obj = go, mat = mat });
        }
    }

    void LateUpdate()
    {
        recordTimer += Time.deltaTime;
        if (recordTimer >= recordInterval)
        {
            recordTimer = 0f;

            // 위치/회전 기록
            if (positionHistory.Count >= ghostCount)
            {
                positionHistory.Dequeue();
                rotationHistory.Dequeue();
            }   
            positionHistory.Enqueue(transform.position);
            rotationHistory.Enqueue(transform.rotation);
        }

        // 잔상 배치
        int idx = 0;
        foreach (var info in ghostPool)
        {
            if (idx < positionHistory.Count)
            {
                info.obj.SetActive(true);
                info.obj.transform.position = GetFromQueue(positionHistory, idx);
                info.obj.transform.rotation = GetFromQueue(rotationHistory, idx);

                if (info.mat != null)
                {
                    var color = info.mat.color;
                    color.a = Mathf.Lerp(0.3f, 0.05f, (float)idx / ghostCount);
                    info.mat.color = color;
                }
            }
            else
            {
                info.obj.SetActive(false);
            }
            idx++;
        }
    }

    // Queue에서 idx번째 값을 가져오는 유틸
    private T GetFromQueue<T>(Queue<T> queue, int idx)
    {
        int i = 0;
        foreach (var item in queue)
        {
            if (i == idx) return item;
            i++;
        }
        return default;
    }
}


