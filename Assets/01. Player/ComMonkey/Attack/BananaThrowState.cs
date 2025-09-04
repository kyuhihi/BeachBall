using UnityEngine;

public class BananaThrowState : StateMachineBehaviour
{

    private MonkeyPlayerMovement monkeyPlayerMovement;
    private float ThrowForce = 1f;
    private float ThrowDirectionY = 1f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (monkeyPlayerMovement == null)
            monkeyPlayerMovement = animator.GetComponentInParent<MonkeyPlayerMovement>();

        monkeyPlayerMovement.MoveByInput = false;
        GameObject CanDoBanana = monkeyPlayerMovement.GetAvailableBanana();
        if (CanDoBanana != null)
        {
            CanDoBanana.transform.position = monkeyPlayerMovement.GetBananaPoint();
            CanDoBanana.transform.rotation = Quaternion.identity;
            CanDoBanana.SetActive(true);
        }
        CanDoBanana.transform.localScale = Vector3.one * 100f;
        CanDoBanana.GetComponent<Rigidbody>().AddForce(animator.transform.forward * ThrowForce + Vector3.up * ThrowDirectionY, ForceMode.Impulse);
        GameSettings.Instance.AddRightAttackSkillCount();

    }


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        monkeyPlayerMovement.MoveByInput = true;

    }


}
