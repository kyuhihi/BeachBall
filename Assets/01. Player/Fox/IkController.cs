using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class IkController : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform LeftHandController;
    [SerializeField]
    private Transform RightHandController;

    [SerializeField, Range(0, 1)]
    private float BlendWeight = 0.0f;
    [SerializeField]
    private Transform Target;

    [SerializeField]
    private RigBuilder rigBuilder;

    [Header("IK 각도 설정")]
    [SerializeField] private float BothHandAngle = 45f;
    [SerializeField] private float OneHandAngle = 135f;
    
    [Header("Smash 애니메이션 설정")]
    [SerializeField] private Vector3 LeftSmashStartPos;
    [SerializeField] private Vector3 RightSmashStartPos;

    public enum PlayerAnimState
    {
        NORMAL,
        L_SMASH_Up2Down,
        R_SMASH_Up2Down,
        Both_SMASH_Up2Down,
        L_SMASH_Down2Up,
        R_SMASH_Down2Up,
        Both_SMASH_Down2Up,

    }
    PlayerAnimState currentAnimState = PlayerAnimState.NORMAL;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        StartCoroutine(EnableRigBuilderAfterDelay(0.5f));
    }
    private System.Collections.IEnumerator EnableRigBuilderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rigBuilder.enabled = true;
    }

    void OnSmash(InputValue value)
    {
        BlendWeight = 1.0f;
        Vector3 toTarget = Target.position - transform.position;
        toTarget.y = 0f;
        float angle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);//y축
        bool isUp2Down = Target.position.y > transform.position.y;

        if (Mathf.Abs(angle) <= BothHandAngle)
        {
            if(isUp2Down)
            {
                currentAnimState = PlayerAnimState.Both_SMASH_Up2Down;
            }
            else
            {
                currentAnimState = PlayerAnimState.Both_SMASH_Down2Up;
            }
        }
        else if (angle < -BothHandAngle && angle > -OneHandAngle)
        {
            if (isUp2Down)
            {
                currentAnimState = PlayerAnimState.L_SMASH_Up2Down;
            }
            else
            {
                currentAnimState = PlayerAnimState.L_SMASH_Down2Up;
            }
        }
        else if (angle > BothHandAngle && angle < OneHandAngle)
        {
            if (isUp2Down)
            {
                currentAnimState = PlayerAnimState.R_SMASH_Up2Down;
            }
            else
            {
                currentAnimState = PlayerAnimState.R_SMASH_Down2Up;
            }
        }
        else
        {
            currentAnimState = PlayerAnimState.NORMAL;
        }

        SetSmashStartPos();
    }

    private void SetSmashStartPos()
    {
        switch (currentAnimState)
        {
            case PlayerAnimState.NORMAL:
                BlendWeight = 0.0f;
                break;
            case PlayerAnimState.L_SMASH_Up2Down:
            case PlayerAnimState.L_SMASH_Down2Up:
                LeftHandController.position = LeftSmashStartPos;
                RightHandController.position = RightSmashStartPos;
                break;
            case PlayerAnimState.R_SMASH_Down2Up:
            case PlayerAnimState.R_SMASH_Up2Down:
                LeftHandController.position = LeftSmashStartPos;
                RightHandController.position = RightSmashStartPos;
                break;
            case PlayerAnimState.Both_SMASH_Down2Up:
            case PlayerAnimState.Both_SMASH_Up2Down:
                LeftHandController.position = LeftSmashStartPos;
                RightHandController.position = RightSmashStartPos;
                break;
        }
    }

    void Update()
    {
        switch (currentAnimState)
        {
            case PlayerAnimState.NORMAL:
                return;
            case PlayerAnimState.L_SMASH_Up2Down:
            case PlayerAnimState.L_SMASH_Down2Up:
                BlendWeight = 1.0f;
                break;
            case PlayerAnimState.R_SMASH_Down2Up:
            case PlayerAnimState.R_SMASH_Up2Down:
                BlendWeight = 1.0f;
                break;
            case PlayerAnimState.Both_SMASH_Down2Up:
            case PlayerAnimState.Both_SMASH_Up2Down:
                BlendWeight = 1.0f;
                break;
        }


        //     Vector3 LeftHandAnimPosition = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        //     Vector3 RightHandAnimPosition = animator.GetBoneTransform(HumanBodyBones.RightHand).position;

        //     LeftHandController.position = Vector3.Lerp(LeftHandAnimPosition, Target.position, leftHandWeight);
        //     RightHandController.position = Vector3.Lerp(RightHandAnimPosition, Target.position, rightHandWeight);
        // }
    }
}
