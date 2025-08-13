using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class TurtlePlayerMovement : BasePlayerMovement
{
    [SerializeField] private ParticleSystem waterCannonParticle;
    [SerializeField] private ParticleSystem waterCannonByEffectParticle;
    [SerializeField] private Transform mouthTransform; // 거북이 입 위치


    protected override void Start()
    {
        base.Start();
        if (mouthTransform != null)
            originalMouthRotation = mouthTransform.rotation;
    }

    private Quaternion originalMouthRotation;
    public override void OnAttackSkill(InputValue value)
    {
        if (value.isPressed)
        {
            // 거북이만의 공격 스킬
            Debug.Log("Turtle: 등껍질 돌진!");
            // 등껍질 돌진 구현
        }
    }
    public override void OnDefenceSkill(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Turtle: 물대포!");

            GameObject ballObj = GameObject.FindWithTag(ballTag);
            if (ballObj != null && waterCannonParticle != null && mouthTransform != null)
            {
                Vector3 ballPos = ballObj.transform.position;
                Vector3 dir = (ballPos - mouthTransform.position).normalized;

                // 현재 머리의 y축(월드) 각도와, 목표 방향의 y축 각도 비교
                float currentY = mouthTransform.eulerAngles.y;
                float targetY = Quaternion.LookRotation(dir).eulerAngles.y;

                // 각도 차이 계산 (0~180)
                float angleDiff = Mathf.DeltaAngle(currentY, targetY);

                float maxAngle = 60f; // 제한 각도

                // 제한 각도 이상이면 물대포 발사 불가
                if (Mathf.Abs(angleDiff) > maxAngle)
                {
                    Debug.Log("너무 돌아가서 물대포 불가!");
                    return;
                }

                if (!waterCannonParticle.isPlaying)
                    originalMouthRotation = mouthTransform.rotation;

                mouthTransform.rotation = Quaternion.LookRotation(dir);

                waterCannonParticle.transform.position = mouthTransform.position;
                waterCannonParticle.transform.rotation = mouthTransform.rotation;
                waterCannonByEffectParticle.transform.position = mouthTransform.position;
                waterCannonByEffectParticle.transform.rotation = mouthTransform.rotation;

                if (waterCannonParticle.isPlaying)
                {
                    waterCannonParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    waterCannonByEffectParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                    MoveByInput = true;
                    mouthTransform.rotation = originalMouthRotation;
                }
                else
                {
                    waterCannonParticle.Play();
                    waterCannonByEffectParticle.Play();
                    MoveByInput = false;
                    StartCoroutine(EnableMoveAfterParticle(waterCannonParticle.main.duration));
                    StartCoroutine(RestoreMouthRotationAfterDelay(waterCannonParticle.main.duration));
                }
            }
        }
    }

    private IEnumerator EnableMoveAfterParticle(float delay)
    {
        yield return new WaitForSeconds(delay);
        MoveByInput = true;
    }

    // 2. 파티클 끝나면 머리 방향 복귀
    private IEnumerator RestoreMouthRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (mouthTransform != null)
        {
            // x=180, y=90, z=0의 로컬 회전값으로 고정
            mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);
        }
    }
}
