using UnityEngine;

public class Banana : MonoBehaviour
{
    private float _CantTouchTime = 0.3f;
    private GameObject HitEffectGameObj;
    private GameObject HitEffectGameObjPrefab;
    private Rigidbody _rb;

    public GameObject BananaParent;
    private void OnEnable()
    {
        _CantTouchTime = 2f;
        HitEffectGameObjPrefab = Resources.Load<GameObject>("BananaHitEffect");
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = Vector3.zero;
    }


    public void Update()
    {
        if (GameManager.GetInstance().CurrentGameState != GameManager.GameState.CUTSCENE)
        {
            if (_CantTouchTime > 0)
            {
                _CantTouchTime -= Time.deltaTime;
            }
        }
    }


    public void OnCollisionEnter(Collision other)
    {
        if (_CantTouchTime > 0)
            return;
        Debug.Log("other name:  " + other.gameObject.tag);
        if (!other.gameObject.CompareTag("Player") && !other.gameObject.CompareTag("SubPlayer"))
        {
            return;
        }
        Vector3 lookAtDir = Vector3.Normalize(transform.position - other.gameObject.transform.position);
        lookAtDir.y = 0f;
        other.gameObject.transform.rotation = Quaternion.LookRotation(lookAtDir);
        other.gameObject.GetComponent<BasePlayerMovement>().Stun(0.5f);

        Rigidbody playerRb = other.gameObject.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.AddForce(-lookAtDir * 5f, ForceMode.Impulse);
        }

        if (HitEffectGameObj == null)
        {
            HitEffectGameObj = Instantiate(HitEffectGameObjPrefab, transform.position, Quaternion.identity);
            HitEffectGameObj.transform.SetParent(BananaParent.transform);
        }
        else
        {
            HitEffectGameObj.transform.position = transform.position;
            HitEffectGameObj.transform.rotation = Quaternion.identity;
            HitEffectGameObj.GetComponent<ParticleSystem>().Play();
        }
        gameObject.SetActive(false);
    }

}
