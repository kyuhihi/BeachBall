using UnityEngine;

public class MonkeyShoulderTarget : MonoBehaviour
{
    Transform target;

    // Update is called once per frame
    void Update()
    {
        if (target == null)
            target = GameObject.FindWithTag("Ball").transform;

            

        transform.position = target.position;

    }
}
