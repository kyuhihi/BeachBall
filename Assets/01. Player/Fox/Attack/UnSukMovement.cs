using Unity.VisualScripting;
using UnityEngine;

public class UnSukMovement : MonoBehaviour
{
    private const string WallLayerName = "Wall and Ground";
    private float m_MovementSpeed = 3.0f;

    private int m_iEffectCnt = 3;
    [SerializeField] private GameObject m_FireEffect;
    [SerializeField] private GameObject m_ExplosionEffect;

    private static bool s_IsQuitting;
    private bool _exploded = false;

    private void OnApplicationQuit() => s_IsQuitting = true;

    // �ʿ� �� ȣ��: ����Ʈ ���� �� ����
    public void ExplodeAndDestroy()
    {
        if (_exploded) return;
        _exploded = true;

        if (m_ExplosionEffect)
            Instantiate(m_ExplosionEffect, transform.position, Quaternion.identity);

        if (CameraShakingManager.Instance != null)
            CameraShakingManager.Instance.DoShake(0.5f, 10f);

        Destroy(gameObject);
    }

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
        if (!CheckTimingCollision(other)) return;

        if (m_iEffectCnt > 0)
        {
            if (m_FireEffect)
                Instantiate(m_FireEffect, other.ClosestPointOnBounds(transform.position), Quaternion.identity);
            --m_iEffectCnt;
        }
        else
        {
            ExplodeAndDestroy(); // Destroy ���� ����Ʈ ����
        }
    }
    private bool CheckTimingCollision(Collider other)
    {
        if (other.GetComponent<UnSukMovement>() != null)
            return false;

        if(other.gameObject.layer == LayerMask.NameToLayer(WallLayerName))
        {
            if (other.gameObject.transform.position.y > 0f)
                return false;
        }

        return true;
    }
    public void OnDestroy()
    {
        // �� ��ε�/�� ����/������ ���� ����� �������� ����
        if (!Application.isPlaying) return;
        if (s_IsQuitting) return;
        if (!gameObject.scene.isLoaded) return;
        if (_exploded) return; // �̹� ó����

        // ���������� Destroy ��ο��� �� ������� ���� ����
        if (m_ExplosionEffect)
            Instantiate(m_ExplosionEffect, transform.position, Quaternion.identity);

        if (CameraShakingManager.Instance != null)
            CameraShakingManager.Instance.DoShake(0.5f, 10f);
    }
}
