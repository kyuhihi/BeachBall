using UnityEngine;
using UnityEngine.InputSystem;

public class HealthBarDriver : MonoBehaviour
{
    public Material mat;
    public float health = 1f;
    float chip = 1f;
    float prev = 1f;
    public float chipLerp = 0.5f;
    public float prevLerp = 1.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 데미지 테스트: Space로 감소
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            health = Mathf.Clamp01(health - 0.15f);
            if (chip < health) chip = health;
        }
        // 회복 테스트: R로 증가
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            health = Mathf.Clamp01(health + 0.1f);
            if (chip < health) chip = health;
        }

        // prev: 한 박자 늦게 따라감
        prev = Mathf.MoveTowards(prev, health, Time.deltaTime * prevLerp);
        // chip: health보다 크면 서서히 health로 내려옴(회복/칩 영역 사라짐)
        chip = Mathf.MoveTowards(chip, health, Time.deltaTime * chipLerp);

        mat.SetFloat("_Health", health);
        mat.SetFloat("_PrevHealth", prev);
        mat.SetFloat("_ChipHealth", chip);
    }
}
