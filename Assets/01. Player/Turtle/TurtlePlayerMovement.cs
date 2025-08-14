using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TurtlePlayerMovement : BasePlayerMovement
{
    [SerializeField] private ParticleSystem waterCannonParticlePrefab; // 프리팹으로 변경
    private ParticleSystem waterCannonParticleInstance;

    [SerializeField] private ParticleSystem waterCannonByEffectParticlePrefab;
    private ParticleSystem waterCannonByEffectParticleInstance;

    [SerializeField] private Transform mouthTransform;

    Vector3 ballPos = Vector3.zero;


    private Quaternion originalMouthRotation;

    protected override void Start()
    {
        base.Start();
        if (mouthTransform != null)
        {
            originalMouthRotation = mouthTransform.rotation;

        }
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
            Debug.Log("Turtle: 물대포!");

            GameObject ballObj = GameObject.FindWithTag(ballTag);
            if (ballObj != null && waterCannonParticlePrefab != null && mouthTransform != null)
            {
                ballPos = ballObj.transform.position;
                Vector3 dir = (ballPos - mouthTransform.position).normalized;

                float currentY = mouthTransform.eulerAngles.y;
                float targetY = Quaternion.LookRotation(dir).eulerAngles.y;
                float angleDiff = Mathf.DeltaAngle(currentY, targetY);
                float maxAngle = 60f;

                if (Mathf.Abs(angleDiff) > maxAngle)
                {
                    Debug.Log("너무 돌아가서 물대포 불가!");
                    return;
                }

                if (waterCannonParticleInstance != null && waterCannonParticleInstance.isPlaying)
                {
                    waterCannonParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    Destroy(waterCannonParticleInstance.gameObject);

                    if (waterCannonByEffectParticleInstance != null)
                    {
                        waterCannonByEffectParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        Destroy(waterCannonByEffectParticleInstance.gameObject);
                    }

                    MoveByInput = true;
                    mouthTransform.rotation = originalMouthRotation;
                }
                else
                {

                    // 입을 Ball 방향으로 회전
                    mouthTransform.rotation = Quaternion.LookRotation(dir);



                    // 파티클 인스턴스 생성
                    waterCannonParticleInstance = Instantiate(
                        waterCannonParticlePrefab,
                        mouthTransform.position,
                        mouthTransform.rotation
                    );
                    waterCannonParticleInstance.Play();

                    // 부가 이펙트도 필요하다면 인스턴스 생성
                    if (waterCannonByEffectParticlePrefab != null)
                    {
                        waterCannonByEffectParticleInstance = Instantiate(
                            waterCannonByEffectParticlePrefab,
                            mouthTransform.position,
                            mouthTransform.rotation
                        );
                        waterCannonByEffectParticleInstance.Play();
                    }




                    MoveByInput = false;
                    StartCoroutine(EnableMoveAfterParticle(waterCannonParticleInstance.main.duration));
                    StartCoroutine(RestoreMouthRotationAfterDelay(waterCannonParticleInstance.main.duration));
                }
            }
        }
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (waterCannonParticleInstance != null && waterCannonParticleInstance.isPlaying)
        {
            // Ball 쪽으로 입과 파티클, 히트박스를 계속 회전
            GameObject ballObj = GameObject.FindWithTag(ballTag);
            if (ballObj != null && mouthTransform != null)
            {
                Vector3 dir = (ballObj.transform.position - mouthTransform.position).normalized;
                mouthTransform.rotation = Quaternion.LookRotation(dir);

                // 파티클 인스턴스도 같은 방향으로 회전
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

    private IEnumerator EnableMoveAfterParticle(float delay)
    {
        yield return new WaitForSeconds(delay);
        mouthTransform.rotation = originalMouthRotation; // 입모양 원래대로 복원

        MoveByInput = true;
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
    }
}