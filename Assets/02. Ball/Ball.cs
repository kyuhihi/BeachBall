using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic; // �߰�

public class Ball : MonoBehaviour
{
    private float speed = 25f;
    private const float MaxSmashSpeed = 30f;
    [SerializeField] private Vector3 direction = Vector3.down;
    private Rigidbody m_Rigidbody;
    [SerializeField]private ParticleSystem m_HitParticle;
    [SerializeField]private ParticleSystem m_LandSpotParticle;
    private SphereCollider m_SphereCollider;
    private int RayLayerMask = 0;

    private float _currentSpeed = 0.0f;
    private const string PlayerTag = "Player";
    private static readonly string[] ScoreTags = { "Ground", "Terrain" };

    public ParticleSystem LandSpotParticle => m_LandSpotParticle;

    private int yFlipThreshold = 8;
    private float yLockDuration = 0.5f;
    private int lastYSign = -1;
    private bool isYLocked = false;
    private Coroutine yLockCoroutine;
    private int wasClamped = 0;

    private float yFlipWindow = 0.3f; // �߰�: ������ ����(��, unscaled)
    private readonly Queue<float> yFlipTimes = new Queue<float>(); // �߰�: ��ȣ���� �ð���

    // Y �������� Ư�� �����̸� Y ��������� ������ -1�� ����
    private float forceDownRangeMinY = 7.8f;
    private float forceDownRangeMaxY = 8f;

    private float scoreInterval = 0.1f;  // ���� �ּ� ����(��)
    private float lastScoreTime = -999f;

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
        if (GameManager.GetInstance().CurrentGameState == GameManager.GameState.CUTSCENE)
        {
            m_Rigidbody.Sleep();
            return;
        }
        else
        {
            m_Rigidbody.WakeUp();
        }
        Movement();

        bool bClamped = GameManager.GetInstance().ConfineObjectPosition(this.gameObject, YOffset: 1.0f);
        if (bClamped)
        {
            if (gameObject.transform.position.y <= -0.1f)
            {
                direction.y = Mathf.Abs(direction.y);
                transform.position = new Vector3(transform.position.x, 1.0f, transform.position.z);
                m_Rigidbody.AddForce(Vector3.up * 5f, ForceMode.Impulse);
            }

        }


    }
    

    private void Movement()
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
            IPlayerInfo.CourtPosition courtPos = otherGO.GetComponent<IPlayerInfo>().m_CourtPosition;
            PlayerUIManager.GetInstance().UpUltimateBar(courtPos); // �ñر� ������ 10 ����
        }
        else
        {
            // ��/���� ���� �� ���� (��ٿ� ����)
            if (otherGO != null && (otherGO.name == ScoreTags[0] || otherGO.name == ScoreTags[1]))
            {
                if (Time.time - lastScoreTime >= scoreInterval)
                {
                    IPlayerInfo.CourtPosition courtPos = contactPoint.z < 0.0f ?
                        IPlayerInfo.CourtPosition.COURT_RIGHT : IPlayerInfo.CourtPosition.COURT_LEFT;

                    PlayerUIManager.GetInstance().UpScore(courtPos);
                    lastScoreTime = Time.time;
                }
            }
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
        direction = AdjustDirectionY(direction);
    }

    private Vector3 AdjustDirectionY(Vector3 dir)
    {



        float sign = Mathf.Sign(dir.y); // ���� ��ȣ ���
        float signZ = Mathf.Sign(dir.z); // ���� ��ȣ ���

        float absY = Mathf.Abs(dir.y);
        float absZ = Mathf.Abs(dir.z);

        if (absY < 0.5f)
        {
            // 0.5 ~ 1 ���� �ȿ��� ���� y ũ�⸦ ����
            float clampedY = Mathf.Clamp(absY, 0.7f, 1f);
            float clampedZ = Mathf.Clamp(absZ, 0.7f, 1f);
            dir.y = clampedY * sign;
            dir.z = clampedZ * signZ;
        }

        return dir.normalized; // ���� ���ʹ� �׻� ����ȭ
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
