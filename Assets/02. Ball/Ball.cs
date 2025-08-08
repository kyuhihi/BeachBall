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
    private const string PlayerTag = "Player";
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
        Vector3 PredictPos = transform.position + direction * speed * Time.deltaTime;
        // 이동 예측 위치에서 penetration 체크
        Collider[] overlaps = Physics.OverlapSphere(PredictPos, m_SphereCollider.radius, RayLayerMask);
        bool corrected = false;

        foreach (var col in overlaps)
        {
            if (col == m_SphereCollider) continue; // 자기 자신은 무시

            Vector3 pushDir;
            float pushDistance;
            bool overlapped = Physics.ComputePenetration(
                m_SphereCollider, PredictPos, transform.rotation,
                col, col.transform.position, col.transform.rotation,
                out pushDir, out pushDistance);

            if (overlapped && pushDistance > 0f)
            {
                PredictPos += pushDir * pushDistance;
                corrected = true;
            }
        }
        transform.position = PredictPos; 
    }

    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.CompareTag(PlayerTag))
        {
            CameraShakingManager.Instance.DoShake(0.1f, 1f);
            HitStopManager.Instance.DoHitStop(0.1f, 0.1f);
        }


        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitDir;
        Wall wall = other.gameObject.GetComponent<Wall>();
        if (wall != null)
        {
            hitDir = wall.GetNormalDirection();
        }
        else
        {
            hitDir = (transform.position - contactPoint).normalized;
        }
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
