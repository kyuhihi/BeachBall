using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
public class TurtlePlayerMovement : BasePlayerMovement
{
    [SerializeField] private ParticleSystem waterCannonParticlePrefab;
    private ParticleSystem waterCannonParticleInstance;

    [SerializeField] private ParticleSystem waterCannonByEffectParticlePrefab;
    private ParticleSystem waterCannonByEffectParticleInstance;

    [SerializeField] private Transform mouthTransform;

    // 거북이 attack skill
    private bool isShellThrowCannonActive = false;

    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform shellHoldPoint; // 손에 들고 있을 위치
    [SerializeField] private Transform shellThrowPoint; // 던질 때 시작 위치
    [SerializeField] private GameObject throwEffectPrefab; // 이펙트 프리팹
    private GameObject heldShell = null;


    Vector3 ballPos = Vector3.zero;

    private bool isWaterCannonActive = false;
    private bool isWaterCannonRotating = false;
    private float waterCannonTurnSpeed = 180f; // 초당 회전 각도
    private float waterCannonAngleThreshold = 5f; // 몇 도 이내면 "완료"로 간주

    

    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();
        m_PlayerType = IPlayerInfo.PlayerType.Turtle;
        m_PlayerDefaultColor = Color.skyBlue;
    }

    public override void OnAttackSkill(InputValue value)
    {
        if (value.isPressed)
        {
            if (isShellThrowCannonActive || isWaterCannonActive || isWaterCannonRotating)
                return;
            isShellThrowCannonActive = true;
            // Debug.Log("Turtle: 등껍질 돌진!");
            MoveByInput = false;

            // 등껍질 미리 생성해서 손에 들고 있게
            if (heldShell == null && shellPrefab != null && shellHoldPoint != null)
            {
                heldShell = Instantiate(shellPrefab, shellHoldPoint.position, shellHoldPoint.rotation, shellHoldPoint);

                GameObject throweffect = Instantiate(throwEffectPrefab, shellThrowPoint.position, shellThrowPoint.rotation);
                Destroy(throweffect, 1f); // 1초 뒤 자동 파괴 (필요시 시간 조절) -> 어차피 알아서 없어짐

            }

            // 애니메이션 트리거
            if (m_Animator != null)
                m_Animator.SetTrigger("ThrowShell");
        }
    }

    public override void OnDefenceSkill(InputValue value)
    {
        if (value.isPressed)
        {
            // 이미 물대포가 진행 중이거나 회전 중이면 무시
            if (isWaterCannonActive || isWaterCannonRotating || isShellThrowCannonActive)
                return;

            isWaterCannonRotating = true;
            MoveByInput = false;
        }
    }
    public override void OnUltimateSkill(InputValue value)
    {
        if (value.isPressed)
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
        }
    }

    public override void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        // m_UltimateFlashGameObject.SetActive(true);
    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        // m_UltimateFlashGameObject.SetActive(false);
    }//이거 오버라이딩해야함.


    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        GameObject ballObj = GameObject.FindWithTag(ballTag);

        // 1. 물대포 회전 중: 몸통/머리 서서히 Ball 쪽으로 회전
        if (isWaterCannonRotating && ballObj != null && mouthTransform != null)
        {
            // 몸통(Y축만)
            Vector3 dir = (ballObj.transform.position - transform.position).normalized;
            Vector3 lookDir = new Vector3(dir.x, 0f, dir.z);
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                float angle = Quaternion.Angle(transform.rotation, Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f));
                float step = waterCannonTurnSpeed * Time.fixedDeltaTime;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f),
                    step
                );

                // 머리(입)
                Vector3 mouthDir = (ballObj.transform.position - mouthTransform.position).normalized;
                Quaternion mouthTargetRot = Quaternion.LookRotation(mouthDir);
                mouthTransform.rotation = Quaternion.RotateTowards(
                    mouthTransform.rotation,
                    mouthTargetRot,
                    step
                );

                // 일정 각도 이내면 애니메이션 트리거
                if (angle < waterCannonAngleThreshold)
                {
                    isWaterCannonRotating = false;
                    isWaterCannonActive = true;
                    if (m_Animator != null)
                        m_Animator.SetTrigger("WaterCannon");
                    m_Animator.SetBool("WaterCannonActive", true);
                }
            }
        }

        // 2. 파티클이 나가는 동안에는 계속 Ball을 따라 회전
        if (waterCannonParticleInstance != null && waterCannonParticleInstance.isPlaying)
        {
            if (ballObj != null && mouthTransform != null)
            {
                Vector3 dir = (ballObj.transform.position - transform.position).normalized;
                Vector3 lookDir = new Vector3(dir.x, 0f, dir.z);
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    float step = waterCannonTurnSpeed * Time.fixedDeltaTime;
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f),
                        step
                    );
                }

                Vector3 mouthDir = (ballObj.transform.position - mouthTransform.position).normalized;
                Quaternion mouthTargetRot = Quaternion.LookRotation(mouthDir);
                float mouthStep = waterCannonTurnSpeed * Time.fixedDeltaTime;
                mouthTransform.rotation = Quaternion.RotateTowards(
                    mouthTransform.rotation,
                    mouthTargetRot,
                    mouthStep
                );

                waterCannonParticleInstance.transform.position = mouthTransform.position;
                waterCannonParticleInstance.transform.rotation = mouthTransform.rotation;

                if (waterCannonByEffectParticleInstance != null)
                {
                    waterCannonByEffectParticleInstance.transform.position = mouthTransform.position;
                    waterCannonByEffectParticleInstance.transform.rotation = mouthTransform.rotation;
                }
            }
        }
    }

    public void ThrowShellAtOpponent()
    {
        // Debug.Log("Turtle: 등껍질 던지기!!!!!!");
        if (heldShell == null)
        {
            // Debug.LogWarning("손에 든 등껍질이 없습니다!");
            return;
        }

        // 상대 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject opponent = null;
        foreach (var player in players)
        {
            if (player != this.gameObject)
            {
                opponent = player;
                break;
            }
        }
        if (opponent == null)
        {
            Debug.LogWarning("상대 플레이어를 찾을 수 없습니다!");
            Destroy(heldShell);
            heldShell = null;
            return;
        }

        // 등껍질을 손에서 떼고 던지기 위치로 이동
        heldShell.transform.SetParent(null);
        if (shellThrowPoint != null)
            heldShell.transform.position = shellThrowPoint.position;

        // 던질 방향 계산
        Vector3 throwDir = (opponent.transform.position - heldShell.transform.position).normalized;

        // Rigidbody로 힘을 가해 던지기
        Rigidbody shellRb = heldShell.GetComponent<Rigidbody>();
        if (shellRb != null)
        {
            float throwForce = 20f; // 원하는 힘으로 조절
            shellRb.isKinematic = false; // 혹시 손에 들 때 kinematic으로 했다면 해제
            shellRb.AddForce(throwDir * throwForce, ForceMode.Impulse);
        }

        MoveByInput = true; // 던진 후 이동 가능
        heldShell = null; // 손에 든 등껍질 비움
        isShellThrowCannonActive = false; // 등껍질 던지기 종료
    }

    // 애니메이션 이벤트에서 호출
    public void FireWaterCannon()
    {
        // Debug.Log("Turtle: 물대포 발사!");
        GameObject ballObj = GameObject.FindWithTag(ballTag);
        if (ballObj != null && waterCannonParticlePrefab != null && mouthTransform != null)
        {
            ballPos = ballObj.transform.position;
            Vector3 dir = (ballPos - mouthTransform.position).normalized;

            mouthTransform.rotation = Quaternion.LookRotation(dir);

            waterCannonParticleInstance = Instantiate(
                waterCannonParticlePrefab,
                mouthTransform.position,
                mouthTransform.rotation
            );
            waterCannonParticleInstance.Play();

            if (waterCannonByEffectParticlePrefab != null)
            {
                waterCannonByEffectParticleInstance = Instantiate(
                    waterCannonByEffectParticlePrefab,
                    mouthTransform.position,
                    mouthTransform.rotation
                );
                waterCannonByEffectParticleInstance.Play();
            }

            StartCoroutine(EnableMoveAfterParticle(waterCannonParticleInstance.main.duration));
            StartCoroutine(RestoreMouthRotationAfterDelay(waterCannonParticleInstance.main.duration));
        }
    }

    private IEnumerator EnableMoveAfterParticle(float delay)
    {
        yield return new WaitForSeconds(delay);
        mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);
    }

    private IEnumerator RestoreMouthRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 파티클 인스턴스 삭제
        if (waterCannonParticleInstance != null)
        {
            Destroy(waterCannonParticleInstance.gameObject);
            waterCannonParticleInstance = null;
        }
        if (waterCannonByEffectParticleInstance != null)
        {
            Destroy(waterCannonByEffectParticleInstance.gameObject);
            waterCannonByEffectParticleInstance = null;
        }

        isWaterCannonActive = false; // 물대포 끝남
        MoveByInput = true;
        if (m_Animator != null)
            m_Animator.SetBool("WaterCannonActive", false);
        mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);
    }
}