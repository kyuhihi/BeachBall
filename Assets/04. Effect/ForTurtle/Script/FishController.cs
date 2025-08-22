using UnityEngine;
using System.Collections;

public class FishController : MonoBehaviour
{
    private Transform waterArea; // UltimateWater의 Transform
    private Vector3 areaMin, areaMax;
    private float moveSpeed = 2f;
    private Vector3 targetPos;

     private Quaternion rotationOffset = Quaternion.identity;

    public void Init(Transform waterArea, Vector3 areaMin, Vector3 areaMax, float moveSpeed = 2f, float yRotationOffset = 0f)
    {
        this.waterArea = waterArea;
        this.areaMin = areaMin;
        this.areaMax = areaMax;
        this.moveSpeed = moveSpeed;
        this.rotationOffset = Quaternion.Euler(0, yRotationOffset, 0); // 추가
        SetNewTarget();
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            SetNewTarget();
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            // 바라보는 방향도 자연스럽게
            Vector3 dir = (targetPos - transform.position).normalized;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir) * rotationOffset;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.1f);
            }
        }
    }

    void SetNewTarget()
    {
        // UltimateWater의 로컬 영역 내에서 랜덤 위치
        float x = Random.Range(areaMin.x, areaMax.x);
        float y = Random.Range(areaMin.y, areaMax.y);
        float z = Random.Range(areaMin.z, areaMax.z);
        targetPos = new Vector3(x, y, z);
    }
}