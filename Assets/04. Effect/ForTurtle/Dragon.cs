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


    private Vector3 startPos;
    private bool isMovingToTarget = false;
    private float moveDuration = 2f; // 이동에 걸리는 시간(초)

    private Vector3 velocity = Vector3.zero;


    private Vector3 moveBeforeUltimateAttack = Vector3.zero; // 드래곤이 공격 전 위치를 이동하기 위한

    public Vector3 MoveBeforeUltimateAttack
    {
        get => moveBeforeUltimateAttack;
        set => moveBeforeUltimateAttack = value;
    }

    private Quaternion dragonRotate = Quaternion.identity; // 플레이어에게 돌진 시 회전 값 유지시키기 위한

    public Quaternion DragonRotate
    {
        get => dragonRotate;
        set => dragonRotate = value;
    }

    private Vector3 drillDirection = Vector3.zero;

    private bool drillMode = false;
    private float drillStartTime = 0f;
    private bool isWaitingForDrill = false;
    private Transform enemyPlayerTransform;

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

        startPos = transform.position;
        isMovingToTarget = true; // 이동 시작

    }

    void Update()
    {
        float time = Time.time * frequency;
        if (!drillMode)
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

            // 목표 위치로 빠르게 접근하다가 느려지며 도착
            if (isMovingToTarget)
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    startPos + moveBeforeUltimateAttack,
                    ref velocity,
                    moveDuration
                );
                // 목표 위치에 충분히 가까워지면 멈춤
                if (Vector3.Distance(transform.position, startPos + moveBeforeUltimateAttack) < 0.01f)
                {
                    isMovingToTarget = false;
                }
            }
            
        }
        else
        {
            // drillMode에서
            if (enemyPlayerTransform != null)
            {
                float drillMoveSpeed = 2f;
                // 1. 이동 방향 계산 (한 번만 계산해서 저장)
                if (drillDirection == Vector3.zero)
                {
                    drillDirection = (enemyPlayerTransform.position - transform.position).normalized;
                    // 드래곤이 플레이어를 바라보게
                    Vector3 lookTarget = transform.position + drillDirection;
                    transform.LookAt(lookTarget);
                    // 필요시 추가 회전
                    transform.Rotate(0, dragonRotate.eulerAngles.y, 0); // 드래곤 모델의 앞 방향에 맞게 각도 조
                }

                // 2. 그 방향으로 계속 이동
                transform.position += drillDirection * drillMoveSpeed * Time.deltaTime;
            }

            // drillMode에서
            float drillSpeed = 1f; // 기본 회전 속도 (조절 가능)
            float drillAccel = 1.2f; // 가속도 (조절 가능)
            float drillElapsed = Time.time - drillStartTime;

            // 회전 각도를 시간의 제곱에 비례해서 증가시킴 (t^2)
            float baseAngle = drillSpeed * drillElapsed + drillAccel * drillElapsed * drillElapsed;

            // 또는 더 빠른 가속감을 원하면 t^3도 가능
            // float baseAngle = drillSpeed * drillElapsed + drillAccel * Mathf.Pow(drillElapsed, 3);

            for (int g = 0; g < vertexGroupsZMove.Count; g++)
            {
                float groupAngle = baseAngle + g * 0.5f;

                foreach (int idx in vertexGroupsZMove[g])
                {
                    Vector3 orig = originalVertices[idx];

                    float radius = Mathf.Sqrt(orig.y * orig.y + orig.z * orig.z);
                    float origAngle = Mathf.Atan2(orig.z, orig.y);
                    float newAngle = origAngle + groupAngle;

                    Vector3 v = orig;
                    v.y = Mathf.Cos(newAngle) * radius;
                    v.z = Mathf.Sin(newAngle) * radius;
                    v.x = orig.x;

                    displacedVertices[idx] = v;
                }
            }
        }
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();
    }

    public void StartDrill()
    {
        // 3. 상대 Player 찾기 (Player 태그, 자기 자신 제외)
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var go in players)
        {
            if (go != this.gameObject)
            {
                enemyPlayerTransform = go.transform;
                break;
            }
        }

        drillMode = true;
        drillStartTime = Time.time;
        AroundDragonParticle.Play();
        AroundDragonParticle2.Play();
        AroundDragonParticle3.Play();
    }
}