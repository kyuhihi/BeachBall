using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class FoxPlayerMovement : BasePlayerMovement
{

    protected override void Start()
    {
        base.Start();
        m_PlayerType = IPlayerInfo.PlayerType.Fox;
        m_PlayerDefaultColor = Color.orange;
    }


    public override void OnAttackSkill(InputValue value)
    {
        if (value.isPressed)
        {
            // 여우만의 공격 스킬
            Debug.Log("Fox: 꼬리 휘두르기!");
            // 꼬리 휘두르기 구현
        }
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
