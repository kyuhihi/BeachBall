using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic; 

public class Ball : MonoBehaviour,IResetAbleListener    
{
    private float speed = 25f;
    private const float MaxSmashSpeed = 30f;
    [SerializeField] private Vector3 direction = Vector3.down;
    private Rigidbody m_Rigidbody;

    [SerializeField]private ParticleSystem m_HitParticle;
    [SerializeField]private ParticleSystem m_LandSpotParticle;


    private SphereCollider m_SphereCollider;
    private int RayLayerMask = 0;

    [SerializeField]private float _currentSpeed = 0.0f;
    private const string PlayerTag = "Player";
    private const string WallAndGroundLayerName = "Wall And Ground";
    private static readonly string[] ScoreTags = { "Ground", "Terrain" };
    private readonly Vector3 InitialRightPosition = new Vector3(-0.83f, 6.24f, -5.76f);

    public ParticleSystem LandSpotParticle => m_LandSpotParticle;
    private float scoreInterval = 0.1f;  // ???? ??? ????(??)
    private float lastScoreTime = -999f;
    [SerializeField]private bool _Stop = false;



    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }
    public void OnRoundStart()
    {
        Application.targetFrameRate = 120;
        IPlayerInfo.CourtPosition lastWinner = GameManager.GetInstance().GetLastWinner();
        Vector3 TargetPosition = InitialRightPosition;
        m_SphereCollider.enabled = true;


        direction = Vector3.down;
        m_Rigidbody.isKinematic = false;
        _Stop = false;
    }

    public void OnRoundEnd()
    {
        _Stop = true;
        IPlayerInfo.CourtPosition lastWinner = GameManager.GetInstance().GetLastWinner();
        Vector3 TargetPosition = InitialRightPosition;

        if (lastWinner == IPlayerInfo.CourtPosition.COURT_LEFT)
        {
            TargetPosition.z = Mathf.Abs(TargetPosition.z);
        }
        transform.position = TargetPosition;
    
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        m_SphereCollider.enabled = false;
        _currentSpeed = 0.0f;
        direction = Vector3.zero;
        m_Rigidbody.isKinematic = true;
    }

    void OnEnable()
    {
        AddResetCall();

    }
    void OnDisable()
    {
        RemoveResetCall();

    }

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();
        //m_Rigidbody.useGravity = false; // �߷� ��Ȱ��ȭ
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
        RayLayerMask =LayerMask.GetMask(WallAndGroundLayerName);

        // Y�� ����
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

    void Update()
    {
        if (_Stop)
            return;

        if (m_Rigidbody.IsSleeping() && GameManager.GetInstance().CurrentGameState == GameManager.GameState.CUTSCENE)
        {
            m_Rigidbody.Sleep();
            return;
        }
        else if (m_Rigidbody.IsSleeping())
        {
            m_Rigidbody.WakeUp();
        }
        Movement();
    }
    void LateUpdate()
    {
        if (_Stop)
            return;

         GameManager.GetInstance().ConfineObjectPosition(this.gameObject, out bool yClamped, out bool zClamped, YOffset: 1.0f);
         if (yClamped)
         {
            if (gameObject.transform.position.y <= -0.5f)
            {
                direction.y = Mathf.Abs(direction.y);
           }
         }

    }

    private void Movement()
    {
        Vector3 PredictPos = gameObject.transform.position + direction * _currentSpeed * Time.deltaTime;

        m_Rigidbody.MovePosition(PredictPos); // Rigidbody ???

        _currentSpeed = Mathf.Lerp(_currentSpeed, speed, Time.deltaTime);
        if (_currentSpeed <= speed)
        {
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0.1f, speed);
        }
        else
        {
            _currentSpeed = Mathf.Clamp(_currentSpeed, speed, MaxSmashSpeed);
        }

    }

    public void OnCollisionEnter(Collision other)
    {
        if (_Stop) return;
        // ?�� ????????? ????? ?? ???? ?? ???? ??? ???
        Vector3 contactPoint = other.contacts[0].point;
        Vector3 hitNormal;
        var wall = other.gameObject.GetComponent<Wall>();
        if (wall != null) hitNormal = wall.GetNormalDirection();
        else              hitNormal = (transform.position - contactPoint).normalized;

        ProcessHit(other.gameObject, other.collider, contactPoint, hitNormal);
    }

    // ??��????? ???? ?????? ????? ?? ??? ??????
    public void ExternalHit(Vector3 contactPoint, Vector3 hitNormal, Collider otherCollider = null, GameObject otherGO = null)
    {
        ProcessHit(otherGO, otherCollider, contactPoint, hitNormal);
    }

    // ?�� ??? ???? ?????(???? OnCollisionEnter ????)
    private void ProcessHit(GameObject otherGO, Collider otherCol, Vector3 contactPoint, Vector3 hitNormal)
    {
        // 1) ?��????? ?�� ???
        if (otherGO != null && otherGO.CompareTag(PlayerTag))
        {
            CameraShakingManager.Instance.DoShake(0.1f, 1f);
            HitStopManager.Instance.DoHitStop(0.1f, 0.1f);
            _currentSpeed = MaxSmashSpeed;
            IPlayerInfo.CourtPosition courtPos = otherGO.GetComponent<IPlayerInfo>().m_CourtPosition;
            PlayerUIManager.GetInstance().UpUltimateBar(courtPos); // ???? ?????? 10 ????
        }
        else
        {
            // ??/???? ???? ?? ???? (???? ????)
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

        // 2) ??? ??? ????(???? ??? ????)
        if (hitNormal == Vector3.zero)
            hitNormal = (transform.position - contactPoint).normalized;

        // 3) ??? ???? ??? ?? Y-?��? ????
        Vector3 newDir = Vector3.Reflect(direction, hitNormal).normalized;

        direction = newDir;

        // 4) ???? ????(??? ???????? ??????)
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
        direction = AdjustDirectionY(direction);

        // 5) ????/???? ????
        if (m_HitParticle != null)
        {
            m_HitParticle.Stop();
            m_HitParticle.transform.position = contactPoint;
            m_HitParticle.Play();
        }
        SetLandSpotParticlePosition();
    }

    private Vector3 AdjustDirectionY(Vector3 dir)
    {
        float sign = Mathf.Sign(dir.y); // ???? ??? ???
        float signZ = Mathf.Sign(dir.z); // ???? ??? ???

        float absY = Mathf.Abs(dir.y);
        float absZ = Mathf.Abs(dir.z);
        var TargetDir = dir; 

        if (absY < 0.5f)
        {
            // 0.5 ~ 1 ???? ????? ???? y ??? ????
            float clampedY = Mathf.Clamp(absY, 0.5f, 1f);
            TargetDir.y = clampedY * sign;
        }
        TargetDir = TargetDir.normalized;

        float clampedZ = Mathf.Clamp(absZ, 0.7f, 1f);
        TargetDir.z = clampedZ * signZ;
        
        return TargetDir; // ???? ????? ??? ?????
    }




}
