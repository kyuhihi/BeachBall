using UnityEngine;

public class Shell : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float hitForce = 10f;
    [SerializeField] private LayerMask wallAndGroundLayer; // WallAndGround 레이어 지정

    [SerializeField] private GameObject hitEffectPrefab; // 이펙트 프리팹
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"Shell collided with: {collision.gameObject.name}");

        // 이펙트 생성 (충돌 지점에)
        if (hitEffectPrefab != null && collision.contacts.Length > 0)
        {
            Vector3 hitPos = collision.contacts[0].point;
            Quaternion hitRot = Quaternion.LookRotation(collision.contacts[0].normal);
            GameObject effect = Instantiate(hitEffectPrefab, hitPos, hitRot);
            Destroy(effect, 2f); // 2초 뒤 자동 파괴 (필요시 시간 조절)
        }

        // WallAndGround 레이어에 닿으면 튕김
        if (((1 << collision.gameObject.layer) & wallAndGroundLayer) != 0)
        {
            if (rb != null)
            {
                Vector3 reflectDir = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
                rb.linearVelocity = reflectDir * rb.linearVelocity.magnitude;
            }
        }
        // 플레이어에 닿으면 효과 적용
        else if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 pushDir = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDir * hitForce, ForceMode.Impulse);
            }

            // 상대가 맞았을 때만 기절 효과
            var playerMovement = collision.gameObject.GetComponent<BasePlayerMovement>();
            if (playerMovement != null && collision.gameObject != this.gameObject)
            {
                playerMovement.Stun(2f); // 2초간 기절, Stun 함수는 BasePlayerMovement에 구현 필요
            }

            Destroy(gameObject);
        }
    }
}