using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;


public class FoxPlayerMovement : BasePlayerMovement
{
    [SerializeField] private Transform m_FireBallSpawnPoint;
    private FireBallContainer m_FireBallContainer;
    
    [Header("Ultimate Setting")]
    const string m_UltimateFlashGameObjName = "UltimateMesh";
    private GameObject m_UltimateFlashGameObject;
    private Material m_UltimateFlashMaterial;
    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();
        m_PlayerType = IPlayerInfo.PlayerType.Fox;
        m_PlayerDefaultColor = Color.orange;

        m_FireBallContainer = GameObject.FindFirstObjectByType<FireBallContainer>();
        int iChildCnt = transform.childCount;
        for (int i = 0; i < iChildCnt; i++)
        {
            if (transform.GetChild(i).name == m_UltimateFlashGameObjName)
            {
                m_UltimateFlashGameObject = transform.GetChild(i).gameObject;
                m_UltimateFlashMaterial = m_UltimateFlashGameObject.GetComponent<ParticleSystemRenderer>().material;
                break;
            }
        }
    }


    public override void OnAttackSkill(InputValue value)
    {
        if (!m_isMoveByInput || m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }

        if (value.isPressed)
        {
            m_Animator.SetTrigger("AttackSkill");
        }
    }

    public void ShootFireBall()//state machine call
    {
        GameObject fireball = m_FireBallContainer.GetPooledFireBall(this.gameObject);
        fireball.GetComponent<FireBall>().ShootFireBall(m_FireBallSpawnPoint, this.gameObject);
    }

    protected override void Update()
    {
        base.Update();
        m_UltimateFlashMaterial.SetFloat("_LocalYClipOffset",transform.position.y);
    }

    public override void OnDefenceSkill(InputValue value)
    {
        if (!m_isMoveByInput && value.isPressed || m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }

        m_Animator.SetBool("DefenceSkill", value.isPressed);


    }

    public override void OnUltimateSkill(InputValue value)
    {
        if (!m_isMoveByInput || m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }

        Vector3 OutPos = Vector3.zero;
        Quaternion OutRot = Quaternion.identity;
        bool bRetVal = GameManager.GetInstance().GetUltimatePos(m_PlayerType, m_CourtPosition, out OutPos, out OutRot);

        if (bRetVal)
        {
            transform.position = OutPos;
            transform.rotation = OutRot;
            m_PlayableDirector.Play();
        }
    }
    public override void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isMoveByInput = false;

        if (playerType == m_PlayerType && courtPosition == m_CourtPosition)
            m_UltimateFlashGameObject.SetActive(true);

    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (!this || gameObject == null || !isActiveAndEnabled) return;

        if (playerType == m_PlayerType && courtPosition == m_CourtPosition)
            m_UltimateFlashGameObject.SetActive(false);

        if (HurtTurtleUltimateSkillStun)
        {
            HurtTurtleUltimateSkillStun = false;
        }
        else
        {
            m_isMoveByInput = true;
        }
    }//이거 오버라이딩해야함.


    protected override void FixedUpdate()
    {
        base.FixedUpdate();


        // 여우의 특수한 움직임이나 기능이 있다면 여기에 추가
        // 예: 빠른 이동, 점프 등
    }

}
