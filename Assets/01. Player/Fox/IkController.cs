using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
    void Update()
    {
        Vector3 toTarget = Target.position - transform.position;
        toTarget.y = 0f;
        float angle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
Debug.Log($"Angle to Target: {angle}");
        float leftHandWeight = 0f;
        float rightHandWeight = 0f;

        if (Mathf.Abs(angle) <= BothHandAngle)
        {
            leftHandWeight = BlendWeight;
            rightHandWeight = BlendWeight;
        }
        else if (angle < -BothHandAngle && angle > -OneHandAngle)
        {
            leftHandWeight = BlendWeight;
            rightHandWeight = 0f;
        }
        else if (angle > BothHandAngle && angle < OneHandAngle)
        {
            leftHandWeight = 0f;
            rightHandWeight = BlendWeight;
        }
        else
        {
            leftHandWeight = 0f;
            rightHandWeight = 0f;
        }

        Vector3 LeftHandAnimPosition = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        Vector3 RightHandAnimPosition = animator.GetBoneTransform(HumanBodyBones.RightHand).position;

        LeftHandController.position = Vector3.Lerp(LeftHandAnimPosition, Target.position, leftHandWeight);
        RightHandController.position = Vector3.Lerp(RightHandAnimPosition, Target.position, rightHandWeight);
    }
}
