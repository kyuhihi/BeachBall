using System;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class Ball : MonoBehaviour, IResetAbleListener
{
    private Rigidbody m_Rigidbody;

    [SerializeField] private ParticleSystem m_HitParticle;
    [SerializeField] private ParticleSystem m_LandSpotParticle;
    private SphereCollider m_SphereCollider;
    private int RayLayerMask = 0;

    private const string PlayerTag = "Player";
    private const string WallAndGroundLayerName = "Wall And Ground";
    private static readonly string[] ScoreTags = { "Ground", "Terrain" };
    private readonly Vector3 InitialRightPosition = new Vector3(-0.83f, 6.24f, -5.76f);

    public ParticleSystem LandSpotParticle => m_LandSpotParticle;
    private float scoreInterval = 0.1f;
    private float lastScoreTime = -999f;
    [SerializeField] private bool _Stop = false;
    [SerializeField] private Vector3 Linearvelocity;
    private GameSettings.SceneType _currentSceneType = GameSettings.SceneType.None;


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
        m_Rigidbody.WakeUp();
        Application.targetFrameRate = 120;

        IPlayerInfo.CourtPosition lastWinner = GameManager.GetInstance().GetLastWinner();
        Vector3 TargetPosition = InitialRightPosition;
        if (lastWinner == IPlayerInfo.CourtPosition.COURT_LEFT)
            TargetPosition.z = Mathf.Abs(TargetPosition.z);
        transform.position = TargetPosition;

        m_SphereCollider.enabled = true;
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.maxLinearVelocity = 20f;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;


        _Stop = false;
    }

    public void OnRoundEnd()
    {
        m_Rigidbody.maxLinearVelocity = 20f;

        _Stop = true;
        IPlayerInfo.CourtPosition lastWinner = GameManager.GetInstance().GetLastWinner();
        Vector3 TargetPosition = InitialRightPosition;
        Linearvelocity = Vector3.zero;
        if (lastWinner == IPlayerInfo.CourtPosition.COURT_LEFT)
        {
            TargetPosition.z = Mathf.Abs(TargetPosition.z);
        }
        transform.position = TargetPosition;

        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        m_SphereCollider.enabled = false;
        m_Rigidbody.isKinematic = true;
        m_Rigidbody.Sleep();
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
        m_Rigidbody.maxLinearVelocity = 20f;
        m_SphereCollider = GetComponent<SphereCollider>();
        if (m_HitParticle != null)
        {
            m_HitParticle = Instantiate(m_HitParticle, transform.position, Quaternion.identity);
            m_HitParticle.Stop();
        }
        if (m_LandSpotParticle != null)
        {
            m_LandSpotParticle = Instantiate(m_LandSpotParticle, transform.position, Quaternion.identity);
            m_LandSpotParticle.Play();
        }
        RayLayerMask = LayerMask.GetMask(WallAndGroundLayerName);

    }
    private void SetLandSpotParticlePosition()
    {
        if (m_Rigidbody == null || m_SphereCollider == null || m_LandSpotParticle == null) return;

        Vector3 vel = m_Rigidbody.linearVelocity;
        if (vel.sqrMagnitude < 0.0001f) return; // ?? ??? ??

        Vector3 dir = vel.normalized;

        if (Physics.SphereCast(transform.position, m_SphereCollider.radius, dir, out RaycastHit hitInfo, 100f, RayLayerMask))
        {
            m_LandSpotParticle.transform.position = hitInfo.point;
            Quaternion rot = Quaternion.LookRotation(hitInfo.normal);
            Vector3 euler = rot.eulerAngles;
            euler.x += 90f;
            m_LandSpotParticle.transform.rotation = Quaternion.Euler(euler);
        }
    }

    void Update()
    {
        if (_currentSceneType == GameSettings.SceneType.None)
            _currentSceneType = GameSettings.Instance.GetSceneType();

        if (_currentSceneType == GameSettings.SceneType.Title)
        {
            return;
        }

        if (GameManager.GetInstance().CurrentGameState == GameManager.GameState.CUTSCENE)
            _Stop = true;
        else
            _Stop = false;

        if (_Stop)
        {
            if (!m_Rigidbody.isKinematic && !m_Rigidbody.IsSleeping())
            {
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.isKinematic = true;
            }
            return;
        }
        else
        {
            if (m_Rigidbody.isKinematic || m_Rigidbody.IsSleeping())
            {
                m_Rigidbody.WakeUp();
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.linearVelocity = Linearvelocity;
            }
            return;
        }
    }
    void LateUpdate()
    {
        if (_Stop) return;

        // ?? ?? ? ?? ?? ????
        Linearvelocity = m_Rigidbody.linearVelocity;

        Linearvelocity.x *= 3f;
        Linearvelocity.x = Mathf.Clamp(Linearvelocity.x, -5f, 5f);
        Linearvelocity.y = Mathf.Clamp(Linearvelocity.y, -15f, 13f);
        Linearvelocity.z *= 10f;
        Linearvelocity.z = Mathf.Clamp(Linearvelocity.z, -8f, 8f);

        m_Rigidbody.linearVelocity = Linearvelocity;

        SetLandSpotParticlePosition();
        if (_currentSceneType != GameSettings.SceneType.Title && _currentSceneType != GameSettings.SceneType.None)
        {
            GameManager.GetInstance().CheckObjectPosition(transform.position, out bool xClamped, out bool yClamped, out bool zClamped);
            ConfineBall(xClamped, yClamped, zClamped);
        }
    }
    private void ConfineBall(bool xClamped, bool yClamped, bool zClamped)
    {
        if (xClamped)
        {
            float fSignValue = -Mathf.Sign(transform.position.x);
            m_Rigidbody.AddForce(new Vector3(fSignValue * 0.5f, 0f, 0f), ForceMode.Impulse);
        }

        if (yClamped)
        {
            if (transform.position.y < -5f)
            {
                IPlayerInfo.CourtPosition lastWinner = GameManager.GetInstance().GetLastWinner();
                Vector3 TargetPosition = InitialRightPosition;
                if (lastWinner == IPlayerInfo.CourtPosition.COURT_LEFT)
                    TargetPosition.z = Mathf.Abs(TargetPosition.z);
                transform.position = TargetPosition;
                PlayerUIManager.GetInstance().SetSystemText(KoreanTextDB.Key.System_Error);
            }
            float fSignValue = -Mathf.Sign(transform.position.y);
            m_Rigidbody.AddForce(new Vector3(0f, fSignValue * 0.5f, 0f), ForceMode.Impulse);
        }

        if (zClamped)
        {
            float fSignValue = -Mathf.Sign(transform.position.z);
            m_Rigidbody.AddForce(new Vector3(0f, 0f, fSignValue * 0.5f), ForceMode.Impulse);
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
        else hitNormal = (transform.position - contactPoint).normalized;

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
        IPlayerInfo.CourtPosition HitcourtPos = contactPoint.z < 0.0f ?
                        IPlayerInfo.CourtPosition.COURT_RIGHT : IPlayerInfo.CourtPosition.COURT_LEFT;

        if (_currentSceneType != GameSettings.SceneType.Title && _currentSceneType != GameSettings.SceneType.None)
        {
            if (otherGO != null && otherGO.CompareTag(PlayerTag))
            {
                CameraShakingManager.Instance.DoShake(0.1f, 1f);
                HitStopManager.Instance.DoHitStop(0.1f, 0.1f);
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

                        PlayerUIManager.GetInstance().UpScore(HitcourtPos);
                        lastScoreTime = Time.time;
                    }
                }
            }
        }
        // 1) ?��????? ?�� ???
        if (otherGO.gameObject.transform.position.y < 5f)
        {
            Vector3 forceVector = Vector3.up * 1.5f;
            m_Rigidbody.AddForce(forceVector, ForceMode.Impulse);
        }
        if (m_HitParticle != null)
        {
            m_HitParticle.Stop();
            m_HitParticle.transform.position = contactPoint;
            m_HitParticle.Play();
        }
    }
}
