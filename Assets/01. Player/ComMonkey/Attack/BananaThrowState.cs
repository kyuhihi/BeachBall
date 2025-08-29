using System.Collections.Generic;
using UnityEngine;

public class BananaThrowState : StateMachineBehaviour
{
    const string BananaPrefabPath = "Banana";
    private GameObject BananaPrefab;
    private MonkeyPlayerMovement monkeyPlayerMovement;
    private float ThrowForce = 1f;
    private float ThrowDirectionY = 1f;

    private int MaxBananaCount = 10;
    private List<GameObject> BananaInstances = new List<GameObject>();
    private GameObject BananaParent;


    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (BananaPrefab == null)
        {
            BananaPrefab = Resources.Load<GameObject>(BananaPrefabPath);
            monkeyPlayerMovement = animator.GetComponentInParent<MonkeyPlayerMovement>();
            MakeBanana();
        }
        monkeyPlayerMovement.MoveByInput = false;
        GameObject CanDoBanana = null;
        foreach (var banana in BananaInstances)
        {
            if (!banana.activeSelf)
            {
                CanDoBanana = banana;
                break;
            }
        }

        if (CanDoBanana != null)
        {
            CanDoBanana.transform.position = monkeyPlayerMovement.GetBananaPoint();
            CanDoBanana.transform.rotation = Quaternion.identity;
            CanDoBanana.SetActive(true);
        }
        else
        {
            CanDoBanana = Instantiate(BananaPrefab, monkeyPlayerMovement.GetBananaPoint(), Quaternion.identity, BananaParent.transform);
            BananaInstances.Add(CanDoBanana);
            CanDoBanana.GetComponent<Banana>().BananaParent = BananaParent;
        }
        CanDoBanana.transform.localScale = Vector3.one * 100f;
        CanDoBanana.GetComponent<Rigidbody>().AddForce(animator.transform.forward * ThrowForce + Vector3.up * ThrowDirectionY, ForceMode.Impulse);
    }

    private void MakeBanana()
    {
        BananaParent = new GameObject("BananaParent");
        BananaParent.transform.position = Vector3.up * 100f;
        BananaParent.transform.rotation = Quaternion.identity;
        for (int i = 0; i < MaxBananaCount; ++i)
        {
            GameObject banana = Instantiate(BananaPrefab, monkeyPlayerMovement.GetBananaPoint(), Quaternion.identity);
            banana.SetActive(false);
            banana.transform.SetParent(BananaParent.transform);
            banana.GetComponent<Banana>().BananaParent = BananaParent;
            BananaInstances.Add(banana);
        }
    }



    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        monkeyPlayerMovement.MoveByInput = true;

    }


}
