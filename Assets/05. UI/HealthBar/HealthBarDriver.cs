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
        // ������ �׽�Ʈ: Space�� ����
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            health = Mathf.Clamp01(health - 0.15f);
            if (chip < health) chip = health;
        }
        // ȸ�� �׽�Ʈ: R�� ����
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            health = Mathf.Clamp01(health + 0.1f);
            if (chip < health) chip = health;
        }

        // prev: �� ���� �ʰ� ����
        prev = Mathf.MoveTowards(prev, health, Time.deltaTime * prevLerp);
        // chip: health���� ũ�� ������ health�� ������(ȸ��/Ĩ ���� �����)
        chip = Mathf.MoveTowards(chip, health, Time.deltaTime * chipLerp);

        mat.SetFloat("_Health", health);
        mat.SetFloat("_PrevHealth", prev);
        mat.SetFloat("_ChipHealth", chip);
    }
}
