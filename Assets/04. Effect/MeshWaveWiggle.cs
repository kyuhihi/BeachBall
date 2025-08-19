using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class MeshWaveWiggle : MonoBehaviour
{
    public float amplitude = 0.0005f;
    public float frequency = 5f;
    public float waveLength = 2.5f;
    public float groupSize = 0.0001f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private List<List<int>> vertexGroups;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh = Instantiate(mesh);
        GetComponent<MeshFilter>().mesh = mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];

        vertexGroups = new List<List<int>>();
        bool[] grouped = new bool[originalVertices.Length];

        int[] sortedIndices = new int[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
            sortedIndices[i] = i;
        System.Array.Sort(sortedIndices, (a, b) => originalVertices[b].x.CompareTo(originalVertices[a].x));

        float firstGroupSize = groupSize * 20;
        float otherGroupSize = groupSize;

        for (int i = 0; i < sortedIndices.Length; )
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
    }
    void Update()
    {
        float time = Time.time * frequency;

        Debug.Log($"Vertex groups count: {vertexGroups.Count}");

        for (int g = 0; g < vertexGroups.Count; g++)
        {
            // 그룹의 대표 x값(첫번째 버텍스의 x값)을 사용
            float groupX = originalVertices[vertexGroups[g][0]].x;
            float phase = (groupX / waveLength) + time + g * 0.1f; // 그룹별로 약간의 오프셋 추가
            float offset = Mathf.Sin(phase) * amplitude;

            foreach (int idx in vertexGroups[g])
            {
                Vector3 v = originalVertices[idx];
                v.y = originalVertices[idx].y + offset;
                displacedVertices[idx] = v;
            }
        }
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
    }
}