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
    public void OnCollisionEnter(Collision collision)
    {
    }

    public void OnCollisionUpdate(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        var wallCol = GetComponent<Collider>();
        var otherCol = other.collider;
        if (wallCol == null || otherCol == null) return;

        // ��ħ(ħ��) ��ŭ�� ��ġ�� �о��
        Vector3 pushDir;
        float pushDist;
        if (Physics.ComputePenetration(
            otherCol, other.transform.position, other.transform.rotation,   // �о ���(�÷��̾�)
            wallCol, wallCol.transform.position, wallCol.transform.rotation, // ����: ��
            out pushDir, out pushDist))
        {
            Vector3 separation = pushDir * (pushDist + 0.001f); // �ణ�� ��Ų ����
            var rb = other.rigidbody;
            if (rb != null)
                rb.position += separation;     // Rigidbody�� ������ position�� ���� ����
            else
                other.transform.position += separation; // ������ Transform �̵�
        }
        Debug.Log($"Wall OnCollisionUpdate: {other.gameObject.name} pushed by {pushDir * pushDist}");
    }
}
