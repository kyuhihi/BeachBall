using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class FoxPlayerMovement : BasePlayerMovement
{
    [SerializeField] private Transform m_FireBallSpawnPoint;
    private FireBallContainer m_FireBallContainer;
    protected override void Start()
    {
        base.Start();
        m_PlayerType = IPlayerInfo.PlayerType.Fox;
        m_PlayerDefaultColor = Color.orange;

        m_FireBallContainer = GameObject.FindFirstObjectByType<FireBallContainer>();
    }
    
    public override void OnAttackSkill(InputValue value)
    {
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

    

    public override void OnDefenceSkill(InputValue value)
    {
        m_Animator.SetBool("DefenceSkill", value.isPressed);


    }

    public override void OnUltimateSkill(InputValue value)
    {
        Debug.Log("유 성 화 산");
    }


    protected override void FixedUpdate()
    {
        base.FixedUpdate();


        // 여우의 특수한 움직임이나 기능이 있다면 여기에 추가
        // 예: 빠른 이동, 점프 등
    }

}
