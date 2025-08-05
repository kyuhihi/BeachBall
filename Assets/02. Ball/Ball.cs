using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    private Vector3 direction;
    private Rigidbody m_Rigidbody;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        // Rigidbody�� ���� �ӵ��� ����ȭ�ؼ� direction�� ����
        if (m_Rigidbody != null && m_Rigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            direction = m_Rigidbody.linearVelocity.normalized;
        }
    }

    void OnCollisionEnter(Collision other)
    {
        Debug.Log("Collision detected with: " + other.gameObject.name);
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitDir = (transform.position - contactPoint).normalized;
        direction = Vector3.Reflect(direction, hitDir).normalized;
        m_Rigidbody.linearVelocity = Vector3.zero; // Reset linear velocity
        direction.y += 0.5f;
        Debug.Log("New direction after collision: " + direction);
        m_Rigidbody.AddForce(direction * speed);
        return;

    }

}
