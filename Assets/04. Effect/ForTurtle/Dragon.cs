using UnityEngine;
using System.Collections.Generic;
using UnityEditor.ShaderKeywordFilter;

[RequireComponent(typeof(MeshFilter))]
public class Dragon : MonoBehaviour
{
    public float amplitude = 0.0005f;
    public float frequency = 5f;
    public float waveLength = 2.5f;
    public float groupSize = 0.0001f;

    private float yCenter; // 전체 y 중앙값


    [SerializeField] private ParticleSystem AroundDragonParticle;

    [SerializeField] private ParticleSystem AroundDragonParticle2;
    [SerializeField] private ParticleSystem AroundDragonParticle3;


    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private List<List<int>> vertexGroups;
    private List<List<int>> vertexGroupsZMove;

    private bool drillMode = false;
    private float drillStartTime = 0f;

    void Start()
    {
        AroundDragonParticle.Stop();
        AroundDragonParticle2.Stop();
        AroundDragonParticle3.Stop();

        mesh = GetComponent<MeshFilter>().mesh;
        mesh = Instantiate(mesh);
        GetComponent<MeshFilter>().mesh = mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];

        // 전체 버텍스 y 중앙값 계산
        float ySum = 0f;
        for (int i = 0; i < originalVertices.Length; i++)
            ySum += originalVertices[i].y;
        yCenter = ySum / originalVertices.Length;



        vertexGroups = new List<List<int>>();
        bool[] grouped = new bool[originalVertices.Length];

        vertexGroupsZMove = new List<List<int>>();
        bool[] groupedZMove = new bool[originalVertices.Length];

        int[] sortedIndices = new int[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
            sortedIndices[i] = i;
        System.Array.Sort(sortedIndices, (a, b) => originalVertices[b].x.CompareTo(originalVertices[a].x));

        float firstGroupSize = groupSize * 20;
        float otherGroupSize = groupSize;

        float firstGroupSizeZMove = groupSize * 10f;
        float otherGroupSizeZMove = groupSize;

        for (int i = 0; i < sortedIndices.Length;)
        {
            int baseIdx = sortedIndices[i];
            float baseX = originalVertices[baseIdx].x;
            List<int> group = new List<int>();
            group.Add(baseIdx);
            grouped[baseIdx] = true;

            int j = i + 1;
            float currentGroupSize = (vertexGroups.Count == 0) ? firstGroupSize : otherGroupSize;

            while (j < sortedIndices.Length)
            {
                int idx = sortedIndices[j];
                if (!grouped[idx] && Mathf.Abs(originalVertices[idx].x - baseX) <= currentGroupSize)
                {
                    group.Add(idx);
                    grouped[idx] = true;
                    j++;
                }
                else
                {
                    break;
                }
            }
            vertexGroups.Add(group);

            i = j;
        }

        for (int i = 0; i < sortedIndices.Length;)
        {
            int baseIdx = sortedIndices[i];
            float baseX = originalVertices[baseIdx].x;
            List<int> groupZMove = new List<int>();
            groupZMove.Add(baseIdx);
            groupedZMove[baseIdx] = true;

            int j = i + 1;
            float currentGroupSizeZMove = (vertexGroupsZMove.Count == 0) ? firstGroupSizeZMove : otherGroupSizeZMove;

            while (j < sortedIndices.Length)
            {
                int idx = sortedIndices[j];
                if (!groupedZMove[idx] && Mathf.Abs(originalVertices[idx].x - baseX) <= currentGroupSizeZMove)
                {
                    groupZMove.Add(idx);
                    groupedZMove[idx] = true;
                    j++;
                }
                else
                {
                    break;
                }
            }
            vertexGroupsZMove.Add(groupZMove);

            i = j;
        }

    }

    void Update()
    {
        float time = Time.time * frequency;
        float transitionTime = 3f;
        float t = 0f;
        if (Time.time > transitionTime)
            t = Mathf.Clamp01((Time.time - transitionTime) / 1.5f); // 1.5초 동안 서서히 중앙값으로

        if (Time.time <= transitionTime)
        {
            // 기존 y 웨이브
            for (int g = 0; g < vertexGroups.Count; g++)
            {
                float groupX = originalVertices[vertexGroups[g][0]].x;
                float phase = (groupX / waveLength) + time + g * 0.1f;
                float offset = Mathf.Sin(phase) * amplitude;

                foreach (int idx in vertexGroups[g])
                {
                    Vector3 v = originalVertices[idx];
                    v.y = originalVertices[idx].y + offset;
                    displacedVertices[idx] = v;
                }
            }
        }
        else
        {

            AroundDragonParticle.Play();
            AroundDragonParticle2.Play();
            AroundDragonParticle3.Play();


            // 3초 후: 각 그룹마다 x축으로 일정한 속도로 회전, 그룹별로 각도 다르게
            float drillSpeed = 5f; // 회전 속도 (라디안/초)
            float baseAngle = (Time.time - transitionTime) * drillSpeed;

            for (int g = 0; g < vertexGroupsZMove.Count; g++)
            {
                // 그룹별 위상차(g * 0.5f)
                float groupAngle = baseAngle + g * 0.5f;

                foreach (int idx in vertexGroupsZMove[g])
                {
                    Vector3 orig = originalVertices[idx];

                    // x축 회전 (y, z만 변형)
                    float radius = Mathf.Sqrt(orig.y * orig.y + orig.z * orig.z);
                    float origAngle = Mathf.Atan2(orig.z, orig.y);
                    float newAngle = origAngle + groupAngle;

                    Vector3 v = orig;
                    v.y = Mathf.Cos(newAngle) * radius;
                    v.z = Mathf.Sin(newAngle) * radius;
                    v.x = orig.x; // x는 그대로

                    displacedVertices[idx] = v;
                }
            }
        }
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();

        //     float time = Time.time * frequency;

        // if (!drillMode)
        // {
        //     // 기존 y 웨이브
        //     for (int g = 0; g < vertexGroups.Count; g++)
        //     {
        //         float groupX = originalVertices[vertexGroups[g][0]].x;
        //         float phase = (groupX / waveLength) + time + g * 0.1f;
        //         float offset = Mathf.Sin(phase) * amplitude;

        //         foreach (int idx in vertexGroups[g])
        //         {
        //             Vector3 v = originalVertices[idx];
        //             v.y = originalVertices[idx].y + offset;
        //             displacedVertices[idx] = v;
        //         }
        //     }
        // }
        // else
        // {
        //     // 드릴 모드: 각 그룹마다 x축으로 일정한 속도로 회전, 그룹별로 각도 다르게
        //     float drillSpeed = 5f;
        //     float baseAngle = (Time.time - drillStartTime) * drillSpeed;

        //     for (int g = 0; g < vertexGroupsZMove.Count; g++)
        //     {
        //         float groupAngle = baseAngle + g * 0.5f;

        //         foreach (int idx in vertexGroupsZMove[g])
        //         {
        //             Vector3 orig = originalVertices[idx];
        //             float radius = Mathf.Sqrt(orig.y * orig.y + orig.z * orig.z);
        //             float origAngle = Mathf.Atan2(orig.z, orig.y);
        //             float newAngle = origAngle + groupAngle;

        //             Vector3 v = orig;
        //             v.y = Mathf.Cos(newAngle) * radius;
        //             v.z = Mathf.Sin(newAngle) * radius;
        //             v.x = orig.x;

        //             displacedVertices[idx] = v;
        //         }
        //     }
        // }
        // mesh.vertices = displacedVertices;
        // mesh.RecalculateNormals();
    }
    
    public void StartDrill()
    {
        drillMode = true;
        drillStartTime = Time.time;
    }
}