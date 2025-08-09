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

    private float _gravityforce = 0;
    private const float Gravity = 9.81f;

    private float _currentSpeed = 0.0f;
    private const string PlayerTag = "Player";

    public ParticleSystem LandSpotParticle => m_LandSpotParticle;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();
        m_Rigidbody.useGravity = false; // ï¿½ß·ï¿½ ï¿½ï¿½ï¿? ï¿½ï¿½ï¿½ï¿½
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
        Vector3 PredictPos = transform.position + direction * _currentSpeed * Time.deltaTime;
        Collider[] overlaps = Physics.OverlapSphere(PredictPos, m_SphereCollider.radius, RayLayerMask);

        foreach (var col in overlaps)
        {
            if (col == m_SphereCollider) continue;

            Vector3 pushDir;
            float pushDistance;
            bool overlapped = Physics.ComputePenetration(
                m_SphereCollider, PredictPos, transform.rotation,
                col, col.transform.position, col.transform.rotation,
                out pushDir, out pushDistance);

            if (overlapped && pushDistance > 0f)
            {
                PredictPos += pushDir * pushDistance;
            }
        }
        _currentSpeed += Time.deltaTime * speed;
        _currentSpeed = Mathf.Clamp(_currentSpeed, 0.0f, speed);
        //PredictPos.y -= _gravityforce;
        transform.position = PredictPos;
        //_gravityforce += Time.deltaTime;
    }

    void OnCollisionEnter(Collision other)
    {
        _gravityforce = 0.0f;
        if (other.gameObject.CompareTag(PlayerTag))
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

        Collider otherCol = other.collider;
        Vector3 pushDir;
        float pushDistance;
        bool overlapped = Physics.ComputePenetration(
            m_SphereCollider, transform.position, transform.rotation,
            otherCol, otherCol.transform.position, otherCol.transform.rotation,
            out pushDir, out pushDistance);

        if (overlapped && pushDistance > 0f)
        {
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
