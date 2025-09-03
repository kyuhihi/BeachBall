using Unity.VisualScripting;
using UnityEngine;

public class UnSukMovement : MonoBehaviour, IResetAbleListener
{
    private const string WallLayerName = "Wall And Ground";
    private float m_MovementSpeed = 3.0f;

    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }

    private int m_iEffectCnt = 3;
    [SerializeField] private GameObject m_FireEffect;
    [SerializeField] private GameObject m_ExplosionEffect;

    private IPlayerInfo.CourtPosition m_OwnerCourtPosition = IPlayerInfo.CourtPosition.COURT_END;

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
    public void OnRoundStart()
    {
        _exploded = true;
        Destroy(gameObject);
    }

    public void OnRoundEnd()
    {
        _exploded = true;
        Destroy(gameObject);
    }
    public void SetOwnerCourtPosition(IPlayerInfo.CourtPosition courtPosition)
    {
        m_OwnerCourtPosition = courtPosition;
        AddResetCall();
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
        if (other.gameObject.tag == "Player")
        {
            if (other.GetComponent<IPlayerInfo>().m_CourtPosition != m_OwnerCourtPosition)
            {
                //enemy
                Vector3 lookAtDir = Vector3.Normalize(transform.position - other.gameObject.transform.position);
                lookAtDir.y = 0f;
                other.gameObject.transform.rotation = Quaternion.LookRotation(lookAtDir);
                other.gameObject.GetComponent<BasePlayerMovement>().Stun(2.0f);

                Rigidbody playerRb = other.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.AddForce(-lookAtDir * 10f, ForceMode.Impulse);
                }
                ExplodeAndDestroy();
            }
            else
            {
                ExplodeAndDestroy();
            }
        }
        else if (!CheckTimingCollision(other)) return;

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
        if (other.GetComponent<UnSukMovement>() != null || other.GetComponent<Ball>() != null)
            return false;

        if (other.gameObject.layer == LayerMask.NameToLayer(WallLayerName))
        {
            if (other.gameObject.name != "Ground")
                return false;
        }
        if (other.tag == "Player")
        {
            if (other.GetComponent<IPlayerInfo>().m_CourtPosition == m_OwnerCourtPosition)
                return false;
            else
            {
                Vector3 lookAtDir = Vector3.Normalize(transform.position - other.gameObject.transform.position);
                lookAtDir.y = 0f;
                other.gameObject.transform.rotation = Quaternion.LookRotation(lookAtDir);
                other.gameObject.GetComponent<BasePlayerMovement>().UltimateStun(2.0f);

                Rigidbody playerRb = other.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.AddForce(-lookAtDir * 10f, ForceMode.Impulse);
                }
            }
        }


            return true;
    }
    public void OnDestroy()
    {
        RemoveResetCall();
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
