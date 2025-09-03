using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
using System;
using NUnit.Framework.Internal;
using UnityEngine.WSA;
using UnityEngine.Animations.Rigging;
using Kyu_BT;
using System.Collections.Generic;

public class MonkeyPlayerMovement : BasePlayerMovement
{
    [Header("Arm Defence")]
    [SerializeField] Transform[] Arm2s = new Transform[2];
    [SerializeField] GameObject target;
    [SerializeField] GameObject stretchPrefab;
    [SerializeField] int stretchCount = 30;
    [SerializeField] float stretchSpacing = 0.3f;
    [SerializeField] Vector3 LinearVelocity = Vector3.zero;
    float stretchAnimTime = 1f;   // 늘어나는 시간
    float shrinkAnimTime = 1f;    // 줄어드는 시간
    [SerializeField] AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 늘어날 때 커브
    [SerializeField] AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 줄어들 때 커브
    [SerializeField] GameObject[] _BoneHands = new GameObject[2];

    [SerializeField] Rig _armRig;
    Vector3 stretchOffset = new Vector3(0, 0, 90);
    GameObject[,] spawnedStretchObjs;
    [SerializeField] private GameObject LastHandPrefab;
    Transform m_BananaSpawnPoint;
    public Vector3 GetBananaPoint() { return m_BananaSpawnPoint.position; }
    float stretchAnimT = 0f;
    float stretchAnimDir = 0f; // 1: 늘리기, -1: 줄이기, 0: 정지
    private bool isStretched = false;
    public bool IsStretching() => isStretched;

    private int MaxBananaCount = 10;
    private List<GameObject> BananaInstances = new List<GameObject>();
    private GameObject BananaParent;
    const string BananaPrefabPath = "Banana";
    private GameObject BananaPrefab;
    private bool m_isCutScene = false;

    protected override void OnInterrupted()
    {
        
    }


    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();
        m_PlayerType = IPlayerInfo.PlayerType.Monkey;
        m_PlayerDefaultColor = Color.black;
        PlayerUIManager.GetInstance().SetPlayerInfoInUI(this);
        m_BananaSpawnPoint = transform.Find("BananaSpawnPoint");
        MakeArm();
        MakeBanana();
    }
    private void MakeBanana()
    {
        if (BananaPrefab == null)
        {
            BananaPrefab = Resources.Load<GameObject>(BananaPrefabPath);
        }
        BananaParent = new GameObject("BananaParent");
        BananaParent.transform.position = Vector3.up * 100f;
        BananaParent.transform.rotation = Quaternion.identity;
        for (int i = 0; i < MaxBananaCount; ++i)
        {
            GameObject banana = Instantiate(BananaPrefab, GetBananaPoint(), Quaternion.identity);
            banana.SetActive(false);
            banana.transform.SetParent(BananaParent.transform);
            banana.GetComponent<Banana>().BananaParent = BananaParent;
            BananaInstances.Add(banana);
        }
    }
    private void MakeArm()
    {
        if (stretchPrefab && Arm2s != null && Arm2s.Length > 0 && stretchCount > 0)
        {
            GameObject ArmParent = new GameObject("ArmParent");
            spawnedStretchObjs = new GameObject[Arm2s.Length, stretchCount + 1];
            for (int armIdx = 0; armIdx < Arm2s.Length; armIdx++)
            {
                var arm = Arm2s[armIdx];
                if (!arm) continue;
                for (int i = 0; i < stretchCount; i++)
                {
                    GameObject obj = Instantiate(stretchPrefab, ArmParent.transform);
                    spawnedStretchObjs[armIdx, i] = obj;

                    if (i == stretchCount - 1)
                    {
                        GameObject Hand = Instantiate(LastHandPrefab, ArmParent.transform);
                        spawnedStretchObjs[armIdx, i + 1] = Hand;
                    }
                }
            }
            foreach (var obj in spawnedStretchObjs)
            {
                if (obj)
                    obj.SetActive(false);
            }
        }

    }

    public override void OnAttackSkill(InputValue value)
    {
        if (!m_isMoveByInput)
        {
            return;
        }


    }
    protected override void Update()
    {
        base.Update();
    }
    public void LateUpdate()
    {
        if (m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            if (isStretched)
            {
                stretchAnimT = 0.0f;
                stretchAnimDir = 0f;
                isStretched = false;
            }
            else
                return;
        }   


        ArmRoutine();
        StretchArm();

        if (isStretched)
        {
            if (_BoneHands[0].activeSelf)
            {
                _BoneHands[0].SetActive(false);
                _BoneHands[1].SetActive(false);
                foreach (var obj in spawnedStretchObjs)
                {
                    if (obj)
                        obj.SetActive(true);
                }
                _armRig.weight = 1.0f;
            }

        }
        else
        {
            if (!_BoneHands[0].activeSelf)
            {
                _BoneHands[0].SetActive(true);
                _BoneHands[1].SetActive(true);
                foreach (var obj in spawnedStretchObjs)
                {
                    if (obj)
                        obj.SetActive(false);
                }
                _armRig.weight = 0.0f;
            }
        }
    }

    private void ArmRoutine()
    {
        // 애니메이션 진행
        if (stretchAnimDir != 0f)
        {
            float animTime = stretchAnimDir > 0 ? stretchAnimTime : shrinkAnimTime;
            stretchAnimT += Time.deltaTime / Mathf.Max(animTime, 0.01f) * stretchAnimDir;
            stretchAnimT = Mathf.Clamp01(stretchAnimT);

            if (stretchAnimT >= 1f)
            {
                stretchAnimT = 1f;
                stretchAnimDir = -1f;
                return;
            }
            else if (stretchAnimT <= 0f)
            {
                stretchAnimT = 0f;
                stretchAnimDir = 0f;
                isStretched = false;
            }
        }
    }
    private void StretchArm()
    {
        if (Arm2s == null || spawnedStretchObjs == null || target == null)
        {
            return;
        }

        bool allArmsFullyStretched = false;

        for (int armIdx = 0; armIdx < Arm2s.Length; armIdx++)
        {
            var arm = Arm2s[armIdx];
            if (!arm) continue;

            Vector3 targetDir = target.transform.position - arm.position;
            float targetDistance = targetDir.magnitude;
            float FinalStretch = 0.0f;

            for (int i = 0; i < stretchCount + 1; i++)
            {
                var obj = spawnedStretchObjs[armIdx, i];
                if (!obj) continue;

                // 커브 적용
                float t = stretchAnimT;
                float curveT = stretchAnimDir >= 0 ? stretchCurve.Evaluate(t) : shrinkCurve.Evaluate(t);
                float spread = Mathf.MoveTowards(0, Mathf.Min(i * stretchSpacing, targetDistance), curveT * Mathf.Min(i * stretchSpacing, targetDistance));
                Vector3 localOffset = new Vector3(-spread, 0, 0);

                Vector3 worldPos = arm.TransformPoint(localOffset);
                obj.transform.position = worldPos;
                obj.transform.rotation = arm.rotation;

                Vector3 objRot = obj.transform.rotation.eulerAngles;
                objRot += stretchOffset;
                obj.transform.rotation = Quaternion.Euler(objRot);

                FinalStretch = spread;
            }
            
            if (stretchAnimDir > 0 && (stretchAnimT >=1.0f && FinalStretch < targetDistance))
            {
                allArmsFullyStretched = true; 
            }

        }

        // 모든 팔이 목표 위치까지 도달했다면
        if (allArmsFullyStretched )
        {
            stretchAnimDir = -1f; 
        }
    }
    public void DefenceByArm(bool bStretch)
    {
        Vector3 lookDir = target.transform.position - transform.position;
        lookDir.y = 0; // Y축 회전만 살리기 위해 수평 방향으로 제한
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }
        // 팔이 이미 변하는 중이라면 리턴
        if (isStretched)
        {
            return;
        }
        if (m_isMoveByInput && bStretch)
            {
                stretchAnimDir = 1f;
                isStretched = true;
            }
            else
            {

                stretchAnimDir = -1f;
            }
    }
    public override void OnDefenceSkill(InputValue value)
    {
        if (!m_isMoveByInput && value.isPressed)
        {
            return;
        }
    }
    public override void OnUltimateSkill(InputValue value)
    {
        if (!m_isMoveByInput)
        {
            return;
        }
        if (!PlayerUIManager.GetInstance().UseAbility(IUIInfo.UIType.UltimateBar, m_CourtPosition))
        {
            return;
        }
    }
    public override void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isCutScene = true;
        m_Movement = Vector2.zero;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_isMoveByInput = false;

        muteFootSfx = true;
        footstepTimer = 0f;
    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isCutScene = false;
        muteFootSfx = false;
        footstepTimer = 0f;
        if (!base.IsStunned)
        {
            m_isMoveByInput = true;
        }
    }//이거 오버라이딩해야함.

    public override void OnRoundStart()
    {
        StopAllCoroutines();
        m_isMoveByInput = true;
        LateUpdate();
        m_Rigidbody.WakeUp();
        GetComponent<CapsuleCollider>().enabled = true;
        m_Rigidbody.isKinematic = false;
        m_Rigidbody.linearVelocity = Vector3.zero;
        SetResetMode();
        m_Movement = Vector2.zero;
    }
    public override void OnRoundEnd()
    {
        StopAllCoroutines();
        m_isMoveByInput = false;

        stretchAnimT = 0.0f;
        stretchAnimDir = 0f;
        isStretched = false;

        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;
        m_Rigidbody.Sleep();
        m_Movement = Vector2.zero;

        isGrounded = false;
        SetResetMode();

        GetComponent<CapsuleCollider>().enabled = false;
        m_eLocomotionState = IdleWalkRunEnum.Idle;
        SetAnimatorParameters(0.0f);
        SetTransformToRoundStart();

    }


    public void ThrowBanana(Transform OtherPlayer)
    {
        if(base.IsStunned || !m_isMoveByInput|| m_isCutScene) return;
        if (OtherPlayer == null || !m_isMoveByInput) return;
        PlayerUIManager UIMgrInstance = PlayerUIManager.GetInstance();
        if (UIMgrInstance.GetCurrentSecond() > 5 && UIMgrInstance.UseAbility(IUIInfo.UIType.UltimateBar, m_CourtPosition))
        {
            Vector3 OutPos = Vector3.zero;
            Quaternion OutRot = Quaternion.identity;
            bool bRetVal = GameManager.GetInstance().GetUltimatePos(m_PlayerType, m_CourtPosition, out OutPos, out OutRot);
            if (bRetVal)
            {
                transform.position = OutPos;
                transform.rotation = OutRot;
                m_PlayableDirector.Play();
            }
            return;
        }

        if (GetAvailableBanana() == null) return;

        Quaternion lookRot = Quaternion.LookRotation(OtherPlayer.position - transform.position, Vector3.up);
        transform.rotation = lookRot;
        m_Animator.SetTrigger("Smash");
    }
    public GameObject GetAvailableBanana()
    {
        foreach (var banana in BananaInstances)
        {
            if (!banana.activeSelf)
            {
                return banana;
            }
        }
        return null;
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        LinearVelocity = m_Rigidbody.linearVelocity;
    }
    public bool IsGroundedForAI() => isGrounded;

    public void AIJump(bool allowDouble = true)
    {
        if (base.IsStunned || !m_isMoveByInput || m_isCutScene) return;
        if (m_eLocomotionState == IdleWalkRunEnum.Swim) return;

        // 1단 점프
        if (isGrounded)
        {
            OnJumpInput(true);
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            return;
        }

        // 더블 점프
        if (allowDouble && !m_IsDoubleJumping && !isGrounded)
        {
            OnDoubleJumpInput(true);
            Vector3 vel = m_Rigidbody.linearVelocity;
            vel.y = 0f;
            m_Rigidbody.linearVelocity = vel;
            m_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

}
