using System;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField]
    public float speed = 5f;
    private Vector3 direction = Vector3.down;
    private Rigidbody m_Rigidbody;
    [SerializeField]private ParticleSystem m_HitParticle;
    [SerializeField]private ParticleSystem m_LandSpotParticle;
    private SphereCollider m_SphereCollider;
    private int RayLayerMask = 0;
    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();
        m_Rigidbody.useGravity = false; // 중력 사용 안함
        if (m_HitParticle != null)
        {
            m_HitParticle = Instantiate(m_HitParticle, transform.position, Quaternion.identity);
            m_HitParticle.Stop();
        }
        if (m_LandSpotParticle != null)
        {
            m_LandSpotParticle = Instantiate(m_LandSpotParticle, transform.position, Quaternion.identity);
            m_LandSpotParticle.Play();
            SetLandSpotParticlePosition();
        }
        RayLayerMask =LayerMask.GetMask("Wall And Ground");
    }
    private void SetLandSpotParticlePosition()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_SphereCollider.radius,direction, out hitInfo, 100f, RayLayerMask))
        {
            m_LandSpotParticle.gameObject.transform.position = hitInfo.point;
            Quaternion rot = Quaternion.LookRotation(hitInfo.normal);
            Vector3 euler = rot.eulerAngles;
            euler.x += 90f;
            m_LandSpotParticle.gameObject.transform.rotation = Quaternion.Euler(euler);
        }
    }

    void LateUpdate()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnCollisionEnter(Collision other)
    {
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitDir = (transform.position - contactPoint).normalized;
        direction = Vector3.Reflect(direction, hitDir).normalized;

        // 겹친 만큼 penetration 계산
        Collider otherCol = other.collider;
        Vector3 pushDir;
        float pushDistance;
        bool overlapped = Physics.ComputePenetration(
            m_SphereCollider, transform.position, transform.rotation,
            otherCol, otherCol.transform.position, otherCol.transform.rotation,
            out pushDir, out pushDistance);

        if (overlapped && pushDistance > 0f)
        {
            // penetration 방향으로 겹친 만큼만 밀어냄
            transform.position += pushDir * pushDistance;
        }

        if (m_HitParticle != null)
        {
            m_HitParticle.Stop();
            m_HitParticle.transform.position = contactPoint;
            m_HitParticle.Play();
        }
        SetLandSpotParticlePosition();
    }

}
