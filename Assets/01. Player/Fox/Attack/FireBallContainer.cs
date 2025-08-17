using UnityEngine;

public class FireBallContainer : MonoBehaviour
{
    [SerializeField] private GameObject m_FireBallPrefab;
    private GameObject[] m_FireBallPool;
    void Start()
    {
        m_FireBallPool = new GameObject[5]; 
        for (int i = 0; i < m_FireBallPool.Length; i++)
        {
            m_FireBallPool[i] = Instantiate(m_FireBallPrefab, transform);
            m_FireBallPool[i].SetActive(false); 
        }
    }

    public GameObject GetPooledFireBall(GameObject owner)
    {

        foreach (GameObject fireball in m_FireBallPool)
        {
            if (!fireball.activeInHierarchy) // Check if the fireball is inactive
            {
                return fireball;
            }
        }
        return null; // No available fireball in the pool
    }
}
