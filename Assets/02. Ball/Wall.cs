using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField]
    private Vector3 direction = Vector3.forward; // �׸� ����
    public Vector3 GetNormalDirection()
    {
        return new Vector3(direction.x, direction.y, direction.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 start = transform.position;
        // ���� ���� �������� ȭ��ǥ�� �׸�
        Vector3 end = start + direction.normalized * 2f;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawRay(end, Quaternion.Euler(0, 20, 0) * -(end - start).normalized * 0.3f);
        Gizmos.DrawRay(end, Quaternion.Euler(0, -20, 0) * -(end - start).normalized * 0.3f);

    }
}
