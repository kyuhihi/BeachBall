using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic; // �߰�

public class Ball : MonoBehaviour
{
    [SerializeField]
    public float speed = 20f;
    private const float MaxSmashSpeed = 30f;
    [SerializeField] private Vector3 direction = Vector3.down;
    private Rigidbody m_Rigidbody;
    [SerializeField]private ParticleSystem m_HitParticle;
    [SerializeField]private ParticleSystem m_LandSpotParticle;
    private SphereCollider m_SphereCollider;
    private int RayLayerMask = 0;

    private float _currentSpeed = 0.0f;
    private const string PlayerTag = "Player";

    public ParticleSystem LandSpotParticle => m_LandSpotParticle;

    // Y���� �ø� ����/���� ����
    [Header("Direction Y Lock")]
    [SerializeField] private int yFlipThreshold = 5;
    [SerializeField] private float yLockDuration = 0.1f;
    private int lastYSign = -1;
    private bool isYLocked = false;
    private Coroutine yLockCoroutine;

    [Tooltip("ª�� �ð� ������(��) ���� Y��ȣ ��ȭ Ƚ���� �Ǵ�")]
    [SerializeField] private float yFlipWindow = 0.1f; // �߰�: ������ ����(��, unscaled)
    private readonly Queue<float> yFlipTimes = new Queue<float>(); // �߰�: ��ȣ���� �ð���

    // Y �������� Ư�� �����̸� Y ��������� ������ -1�� ����
    [Header("Y ������ ���� ���� �ϰ�")]
    [SerializeField] private float forceDownRangeMinY = 7.8f;
    [SerializeField] private float forceDownRangeMaxY = 8f;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();
        m_Rigidbody.useGravity = false; // �߷� ���? ����
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

        // �ʱ� y��ȣ ���
        lastYSign = direction.y >= 0f ? 1 : -1;
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
        // ��� y ���� ���� ����
        if (isYLocked && direction.y > 0f)
        {
            direction = new Vector3(direction.x, -Mathf.Abs(direction.y), direction.z).normalized;
        }

        // Y�� 7~8 ������ ������ Y ��������� -1�� ����
        float yPos = transform.position.y;
        if (yPos >= forceDownRangeMinY && yPos <= forceDownRangeMaxY)
        {
            direction = new Vector3(direction.x, -1f, direction.z);
        }

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
        _currentSpeed = Mathf.Lerp(_currentSpeed, speed, Time.deltaTime);
        if (_currentSpeed <= speed)
        {
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0.0f, speed);
        }
        else
        {
            _currentSpeed = Mathf.Clamp(_currentSpeed, speed, MaxSmashSpeed);
        }

        transform.position = PredictPos;
    }

    public void OnCollisionEnter(Collision other)
    {
        // �浹 �����Ϳ��� �ʿ��� �� ���� �� ���� ó�� ȣ��
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitNormal;
        var wall = other.gameObject.GetComponent<Wall>();
        if (wall != null) hitNormal = wall.GetNormalDirection();
        else              hitNormal = (transform.position - contactPoint).normalized;

        ProcessHit(other.gameObject, other.collider, contactPoint, hitNormal);
    }

    // �ܺο����� ���� ������ ȣ���� �� �ִ� ������
    public void ExternalHit(Vector3 contactPoint, Vector3 hitNormal, Collider otherCollider = null, GameObject otherGO = null)
    {
        ProcessHit(otherGO, otherCollider, contactPoint, hitNormal);
    }

    // �浹 ó�� ���� �޼���(���� OnCollisionEnter ����)
    private void ProcessHit(GameObject otherGO, Collider otherCol, Vector3 contactPoint, Vector3 hitNormal)
    {
        // 1) �÷��̾�� �浹 ȿ��
        if (otherGO != null && otherGO.CompareTag(PlayerTag))
        {
            CameraShakingManager.Instance.DoShake(0.1f, 1f);
            HitStopManager.Instance.DoHitStop(0.1f, 0.1f);
            _currentSpeed = MaxSmashSpeed;
        }

        // 2) ��Ʈ ��� ����(���� ��� ����)
        if (hitNormal == Vector3.zero)
            hitNormal = (transform.position - contactPoint).normalized;

        // 3) �ݻ� ���� ��� �� Y-�ø� ����
        Vector3 newDir = Vector3.Reflect(direction, hitNormal).normalized;

        int newSign = newDir.y >= 0f ? 1 : -1;
        if (newSign != lastYSign)
        {
            lastYSign = newSign;
            RegisterYFlipTime();
        }
        if (ShouldLockYByFlipBurst())
        {
            newDir = new Vector3(newDir.x, -Mathf.Abs(newDir.y), newDir.z).normalized;
            if (yLockCoroutine != null) StopCoroutine(yLockCoroutine);
            yLockCoroutine = StartCoroutine(LockYNegativeFor(yLockDuration));
            yFlipTimes.Clear();
            lastYSign = -1;
            Debug.Log($"Y Flip Burst Detected! Locking Y for {yLockDuration} seconds.");
        }
        direction = newDir;

        // 4) ���� ����(��� �ݶ��̴��� ������)
        if (otherCol != null)
        {
            Vector3 pushDir;
            float pushDistance;
            bool overlapped = Physics.ComputePenetration(
                m_SphereCollider, transform.position, transform.rotation,
                otherCol, otherCol.transform.position, otherCol.transform.rotation,
                out pushDir, out pushDistance);
            if (overlapped && pushDistance > 0f)
                transform.position += pushDir * pushDistance;
        }

        // 5) ��ƼŬ/���� ����
        if (m_HitParticle != null)
        {
            m_HitParticle.Stop();
            m_HitParticle.transform.position = contactPoint;
            m_HitParticle.Play();
        }
        SetLandSpotParticlePosition();
    }

    private System.Collections.IEnumerator LockYNegativeFor(float duration)
    {
        isYLocked = true;
        float end = Time.time + duration;
        while (Time.time < end)
        {
            // � ��ηε� ����� �Ǹ� ��� ������ ����
            if (direction.y > 0f)
                direction = new Vector3(direction.x, -Mathf.Abs(direction.y), direction.z).normalized;
            yield return null;
        }
        isYLocked = false;
    }

    // �߰�: ��ȣ ���� �ð��� ����ϰ�, ������ ���� �׸� ����
    private void RegisterYFlipTime()
    {
        float now = Time.unscaledTime; // HitStop ����
        yFlipTimes.Enqueue(now);
        // ������ ���� ������ �̺�Ʈ ����
        while (yFlipTimes.Count > 0 && now - yFlipTimes.Peek() > yFlipWindow)
            yFlipTimes.Dequeue();
    }

    // �߰�: ���� ������ �ȿ����� �ø� Ƚ���� �Ӱ�ġ�� �Ѵ���
    private bool ShouldLockYByFlipBurst()
    {
        float now = Time.unscaledTime;
        while (yFlipTimes.Count > 0 && now - yFlipTimes.Peek() > yFlipWindow)
            yFlipTimes.Dequeue();
        return yFlipTimes.Count >= yFlipThreshold;
    }

}
