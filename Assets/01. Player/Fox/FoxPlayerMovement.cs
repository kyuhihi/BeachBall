using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class FoxPlayerMovement : BasePlayerMovement
{
    protected override void Start()
    {
        base.Start();

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
        if (value.isPressed)
        {
            // 여우만의 방어 스킬
            Debug.Log("Fox: 빠른 회피!");
            // 회피 구현
        }
    }
}
