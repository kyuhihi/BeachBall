using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField]
    private Vector3 direction = Vector3.forward; // 그릴 방향
    public Vector3 GetNormalDirection()
    {
        return new Vector3(direction.x, direction.y, direction.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 start = transform.position;
        // 월드 기준 방향으로 화살표를 그림
        Vector3 end = start + direction.normalized * 2f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawRay(end, Quaternion.Euler(0, 20, 0) * -(end - start).normalized * 0.3f);
        Gizmos.DrawRay(end, Quaternion.Euler(0, -20, 0) * -(end - start).normalized * 0.3f);
    }
    public void OnCollisionEnter(Collision collision)
    {
    }

    public void OnCollisionUpdate(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        var wallCol = GetComponent<Collider>();
        var otherCol = other.collider;
        if (wallCol == null || otherCol == null) return;

        // 겹침(침투) 만큼만 위치로 밀어내기
        Vector3 pushDir;
        float pushDist;
        if (Physics.ComputePenetration(
            otherCol, other.transform.position, other.transform.rotation,   // 밀어낼 대상(플레이어)
            wallCol, wallCol.transform.position, wallCol.transform.rotation, // 고정: 벽
            out pushDir, out pushDist))
        {
            Vector3 separation = pushDir * (pushDist + 0.001f); // 약간의 스킨 여유
            var rb = other.rigidbody;
            if (rb != null)
                rb.position += separation;     // Rigidbody가 있으면 position로 직접 보정
            else
                other.transform.position += separation; // 없으면 Transform 이동
        }
        Debug.Log($"Wall OnCollisionUpdate: {other.gameObject.name} pushed by {pushDir * pushDist}");
    }
}
