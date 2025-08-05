using System;
using UnityEngine;

public class Ball : MonoBehaviour
{
    public float speed = 5f;
    public float PlusdirectionY = 1.0f;
    private Vector3 direction = Vector3.down;
    private Rigidbody m_Rigidbody;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();

    }

    void LateUpdate()
    {
        // Rigidbody의 현재 속도를 정규화해서 direction에 저장
        if (m_Rigidbody != null && m_Rigidbody.linearVelocity.sqrMagnitude > 0.0001f)
        {
            direction = m_Rigidbody.linearVelocity.normalized;
        }
        
    }

    void OnCollisionEnter(Collision other)
    {
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitDir = (transform.position - contactPoint).normalized;
        direction = Vector3.Reflect(direction, hitDir).normalized;
        direction.y += PlusdirectionY; // Y축 방향 조정 
                                       //Debug.Log("Ball hit: " + other.gameObject.name + " at " + contactPoint + " with direction: " + direction);
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.AddForce(direction * speed);

    }

}
