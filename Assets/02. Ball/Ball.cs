using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic; // 추가

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

    // Y방향 플립 감지/고정 관련
    [Header("Direction Y Lock")]
    [SerializeField] private int yFlipThreshold = 5;
    [SerializeField] private float yLockDuration = 0.1f;
    private int lastYSign = -1;
    private bool isYLocked = false;
    private Coroutine yLockCoroutine;

    [Tooltip("짧은 시간 윈도우(초) 안의 Y부호 변화 횟수로 판단")]
    [SerializeField] private float yFlipWindow = 0.1f; // 추가: 윈도우 길이(초, unscaled)
    private readonly Queue<float> yFlipTimes = new Queue<float>(); // 추가: 부호변경 시각들

    // Y 포지션이 특정 구간이면 Y 진행방향을 강제로 -1로 설정
    [Header("Y 포지션 구간 강제 하강")]
    [SerializeField] private float forceDownRangeMinY = 7.8f;
    [SerializeField] private float forceDownRangeMaxY = 8f;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();
        m_Rigidbody.useGravity = false; // 占쌩뤄옙 占쏙옙占? 占쏙옙占쏙옙
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

        // 초기 y부호 기록
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
        // 잠시 y 음수 고정 유지
        if (isYLocked && direction.y > 0f)
        {
            direction = new Vector3(direction.x, -Mathf.Abs(direction.y), direction.z).normalized;
        }

        // Y가 7~8 구간에 있으면 Y 진행방향을 -1로 강제
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
        // 충돌 데이터에서 필요한 값 추출 후 공용 처리 호출
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitNormal;
        var wall = other.gameObject.GetComponent<Wall>();
        if (wall != null) hitNormal = wall.GetNormalDirection();
        else              hitNormal = (transform.position - contactPoint).normalized;

        ProcessHit(other.gameObject, other.collider, contactPoint, hitNormal);
    }

    // 외부에서도 동일 로직을 호출할 수 있는 진입점
    public void ExternalHit(Vector3 contactPoint, Vector3 hitNormal, Collider otherCollider = null, GameObject otherGO = null)
    {
        ProcessHit(otherGO, otherCollider, contactPoint, hitNormal);
    }

    // 충돌 처리 공용 메서드(원래 OnCollisionEnter 내용)
    private void ProcessHit(GameObject otherGO, Collider otherCol, Vector3 contactPoint, Vector3 hitNormal)
    {
        // 1) 플레이어와 충돌 효과
        if (otherGO != null && otherGO.CompareTag(PlayerTag))
        {
            CameraShakingManager.Instance.DoShake(0.1f, 1f);
            HitStopManager.Instance.DoHitStop(0.1f, 0.1f);
            _currentSpeed = MaxSmashSpeed;
        }

        // 2) 히트 노멀 보정(없을 경우 추정)
        if (hitNormal == Vector3.zero)
            hitNormal = (transform.position - contactPoint).normalized;

        // 3) 반사 방향 계산 및 Y-플립 제어
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

        // 4) 관통 보정(상대 콜라이더가 있으면)
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

        // 5) 파티클/랜드 스팟
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
            // 어떤 경로로든 양수가 되면 즉시 음수로 유지
            if (direction.y > 0f)
                direction = new Vector3(direction.x, -Mathf.Abs(direction.y), direction.z).normalized;
            yield return null;
        }
        isYLocked = false;
    }

    // 추가: 부호 변경 시각을 기록하고, 윈도우 밖의 항목 제거
    private void RegisterYFlipTime()
    {
        float now = Time.unscaledTime; // HitStop 무시
        yFlipTimes.Enqueue(now);
        // 윈도우 밖의 오래된 이벤트 제거
        while (yFlipTimes.Count > 0 && now - yFlipTimes.Peek() > yFlipWindow)
            yFlipTimes.Dequeue();
    }

    // 추가: 현재 윈도우 안에서의 플립 횟수가 임계치를 넘는지
    private bool ShouldLockYByFlipBurst()
    {
        float now = Time.unscaledTime;
        while (yFlipTimes.Count > 0 && now - yFlipTimes.Peek() > yFlipWindow)
            yFlipTimes.Dequeue();
        return yFlipTimes.Count >= yFlipThreshold;
    }

}
