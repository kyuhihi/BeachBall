using System;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    float speed = 1.5f;


    // Update is called once per frame
    void Update()
    {
        Vector3 CloudPosition = transform.position;
        CloudPosition.y += (float)Math.Sin(Time.time) * 0.01f; // 구름의 Y축 위치를 사인파로 변경
        CloudPosition.z -= speed * Time.deltaTime;
        transform.position = CloudPosition;
        Vector3 localScale = transform.localScale;
        localScale *= 1.0f + Mathf.Sin(Time.time) * 0.0005f;
        transform.localScale = localScale; // 구름의 크기를 사인파로 변경
    }


}
