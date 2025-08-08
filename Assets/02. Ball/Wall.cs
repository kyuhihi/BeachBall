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
}
