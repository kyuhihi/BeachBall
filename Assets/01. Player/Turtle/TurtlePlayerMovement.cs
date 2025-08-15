using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TurtlePlayerMovement : BasePlayerMovement
{
    [SerializeField] private ParticleSystem waterCannonParticlePrefab;
    private ParticleSystem waterCannonParticleInstance;

    [SerializeField] private ParticleSystem waterCannonByEffectParticlePrefab;
    private ParticleSystem waterCannonByEffectParticleInstance;

    [SerializeField] private Transform mouthTransform;
    

    Vector3 ballPos = Vector3.zero;

    private bool isWaterCannonActive = false;
    private bool isWaterCannonRotating = false;
    private float waterCannonTurnSpeed = 180f; // 초당 회전 각도
    private float waterCannonAngleThreshold = 5f; // 몇 도 이내면 "완료"로 간주


    protected override void Start()
    {
        base.Start();
        
    }

    public override void OnAttackSkill(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Turtle: 등껍질 돌진!");
        }
    }

    public override void OnDefenceSkill(InputValue value)
    {
        if (value.isPressed)
        {
            // 이미 물대포가 진행 중이거나 회전 중이면 무시
            if (isWaterCannonActive || isWaterCannonRotating)
                return;

            isWaterCannonRotating = true;
            MoveByInput = false;
        }
    }

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

    // 애니메이션 이벤트에서 호출
    public void FireWaterCannon()
    {
        Debug.Log("Turtle: 물대포 발사!");
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