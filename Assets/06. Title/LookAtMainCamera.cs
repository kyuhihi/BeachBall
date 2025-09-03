using UnityEngine;

public class LookAtMainCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            // 카메라 위치 방향으로 오브젝트의 Y축이 바라보게 회전
            Vector3 dir = cam.transform.position - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                // Y축이 바라보게: LookRotation에서 up 방향에 dir을 넣음
                transform.rotation = Quaternion.LookRotation(transform.forward, dir.normalized);
            }
        }
    }
}