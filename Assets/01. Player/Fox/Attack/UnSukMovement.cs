using UnityEngine;

public class UnSukMovement : MonoBehaviour
{
    private float m_MovementSpeed = 3.0f;

    private int m_iEffectCnt = 3;
    [SerializeField] private GameObject m_FireEffect;
    [SerializeField] private GameObject m_ExplosionEffect;



    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < -1f)
        {
            Destroy(gameObject);
            return;
        }
        // Move the object forward at a constant speed
        transform.Translate(Vector3.forward * m_MovementSpeed * Time.deltaTime);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<UnSukMovement>() != null)
            return;
        if (m_iEffectCnt > 0)
            {
                Instantiate(m_FireEffect, other.ClosestPointOnBounds(transform.position), Quaternion.identity);
                --m_iEffectCnt;
            }
            else
            {
                Destroy(gameObject);
            }

    }
}
