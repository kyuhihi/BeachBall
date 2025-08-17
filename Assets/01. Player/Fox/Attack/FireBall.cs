using System.Collections.Generic;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float turnSpeedDegPerSec = 360f; // 초당 회전 속도(도)

    private float m_DisappearTime = 0.0f;
    
    private GameObject m_Target;
    private List<GameObject> m_Players = new List<GameObject>();

    private void OnEnable()
    {
        m_DisappearTime = 2.0f;
    }
    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        m_Players.AddRange(players);
    }

    private void Update()
    {
        // 1) 타깃이 있으면 그 방향으로 서서히 회전
        if (m_Target != null && m_DisappearTime > 1.0f)
        {
            Vector3 TargetOffset = m_Target.transform.position;
            TargetOffset.y += 0.5f;
            Vector3 toTarget = TargetOffset - transform.position;
            if (toTarget.sqrMagnitude > 1e-6f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot, turnSpeedDegPerSec * Time.deltaTime);
            }
        }
        // 2) 항상 현재 forward로 전진
        transform.position += transform.forward * speed * Time.deltaTime;

        m_DisappearTime -= Time.deltaTime;
        if (m_DisappearTime <= 0f)
        {
            gameObject.SetActive(false); // Deactivate the fireball
        }
    }

    public void ShootFireBall(Transform TargetStartPosition, GameObject owner)
    {
        foreach (GameObject player in m_Players)
        {
            if (player != owner)
            {
                m_Target = player; // Set the target to the first player found
                break;
            }
        }
        this.gameObject.SetActive(true);
        transform.position = TargetStartPosition.position;
        transform.rotation = TargetStartPosition.rotation;
    }

    public void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player") && (other.gameObject == m_Target))
        {
            // Handle damage to the enemy
            gameObject.SetActive(false); // Deactivate the fireball on hit
        }
    }


}
