using UnityEngine;

public class BananaThrowState : StateMachineBehaviour
{
    const string BananaPrefabPath = "Banana";
    private GameObject BananaPrefab;
    private MonkeyPlayerMovement monkeyPlayerMovement;
    private float ThrowForce = 30f;
    private float ThrowDirectionY = 2.5f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (BananaPrefab == null)
        {
            BananaPrefab = Resources.Load<GameObject>(BananaPrefabPath);
            monkeyPlayerMovement = animator.GetComponentInParent<MonkeyPlayerMovement>();
        }
        monkeyPlayerMovement.MoveByInput = false;
        //monkeyPlayerMovement.GetBananaPoint();
        GameObject Banana = Instantiate(BananaPrefab, monkeyPlayerMovement.GetBananaPoint(), Quaternion.identity);
        Banana.transform.localScale = Vector3.one * 100f;
        Banana.GetComponent<Rigidbody>().AddForce(animator.transform.forward * ThrowForce + Vector3.up * ThrowDirectionY, ForceMode.Impulse);
    }


    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        monkeyPlayerMovement.MoveByInput = true;

    }


}
