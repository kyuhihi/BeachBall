using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
using System;

public class MonkeyPlayerMovement : BasePlayerMovement
{
    [Header("Arm Defence")]
    [SerializeField] Transform[] Arm2s = new Transform[2];
    [SerializeField] GameObject target;
    [SerializeField] GameObject stretchPrefab;
    [SerializeField] int stretchCount = 30;
    [SerializeField] float stretchSpacing = 0.3f;
     float stretchAnimTime = 1f;   // 늘어나는 시간
    float shrinkAnimTime = 1f;    // 줄어드는 시간
    [SerializeField] AnimationCurve stretchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 늘어날 때 커브
    [SerializeField] AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // 줄어들 때 커브
    [SerializeField] Key stretchKey = Key.Q;
    Vector3 stretchOffset = new Vector3(0, 0, 90);
    GameObject[,] spawnedStretchObjs;
    float stretchAnimT = 0f;
    float stretchAnimDir = 0f; // 1: 늘리기, -1: 줄이기, 0: 정지
    bool isStretched = false;
    public bool IsFullyStretched => stretchAnimT >= 1f && stretchAnimDir == 0f;

    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();
        m_PlayerType = IPlayerInfo.PlayerType.Monkey;
        m_PlayerDefaultColor = Color.black;
        PlayerUIManager.GetInstance().SetPlayerInfoInUI(this);
        MakeArm();
    }

    private void MakeArm()
    {
        if (stretchPrefab && Arm2s != null && Arm2s.Length > 0 && stretchCount > 0)
        {
            GameObject ArmParent = new GameObject("ArmParent");
            spawnedStretchObjs = new GameObject[Arm2s.Length, stretchCount];
            for (int armIdx = 0; armIdx < Arm2s.Length; armIdx++)
            {
                var arm = Arm2s[armIdx];
                if (!arm) continue;
                for (int i = 0; i < stretchCount; i++)
                {
                    GameObject obj = Instantiate(stretchPrefab, ArmParent.transform);
                    spawnedStretchObjs[armIdx, i] = obj;
                }
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
        ArmRoutine();
        StretchArm();
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
                Debug.Log("Auto Shrink");
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

            for (int i = 0; i < stretchCount; i++)
            {
                var obj = spawnedStretchObjs[armIdx, i];
                if (!obj) continue;

                // 커브 적용
                float t = stretchAnimT;
                float curveT = stretchAnimDir >= 0 ? stretchCurve.Evaluate(t) : shrinkCurve.Evaluate(t);
                float spread = Mathf.Lerp(0, Mathf.Min(i * stretchSpacing, targetDistance), curveT);
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
                Debug.Log($"팔 {armIdx}의 최종 늘어남: {FinalStretch} < {targetDistance}");
                allArmsFullyStretched = true; // 모든 팔이 목표 위치까지 도달하지 못함
            }

        }

        // 모든 팔이 목표 위치까지 도달했다면
        if (allArmsFullyStretched )
        {
            Debug.Log("모든 팔이 목표 위치까지 뻗었습니다!");
            stretchAnimDir = -1f; // 자동으로 줄이기 시작
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
                Debug.Log("StretchCall");
                stretchAnimDir = 1f;
                isStretched = true;
            }
            else
            {
                Debug.Log("ShrinkCall");

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
        m_isMoveByInput = false;
    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isMoveByInput = true;
    }//이거 오버라이딩해야함.


    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }
    public bool IsGroundedForAI() => isGrounded;

    public void AIJump(bool allowDouble = true)
    {
        if (!m_isMoveByInput) return;
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
