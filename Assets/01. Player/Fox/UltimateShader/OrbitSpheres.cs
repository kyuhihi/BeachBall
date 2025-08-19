using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class OrbitSpheres : MonoBehaviour
{
    public GameObject spherePrefab;   // 생성할 구체 프리팹
    private Transform player;          // 중심이 되는 플레이어
    public int sphereCount = 20;      // 구체 개수
    public float radius = 3f;         // 공전 반경
    public float orbitSpeed = 30f;    // 공전 속도 (deg/sec)
    public float lerpSpeed = 5f;      // 부드럽게 따라오는 정도

    private List<Transform> spheres = new List<Transform>();
    private ORBIT_STATE m_OrbitState = ORBIT_STATE.ORBIT_STANDBY;
    // 발사 시퀀스 설정
    [Header("Shoot Settings")]
    [SerializeField] private float shootInterval = 0.001f;          // 몇 초 간격으로 다음 구체 발사
    [SerializeField] private float shootDuration = 0.6f;            // 목표 지점까지 걸리는 시간
    [SerializeField] private float shootTargetOffsetRadius = 0.35f; // 카메라 주변 랜덤 오프셋 반경
    [SerializeField] private AnimationCurve shootCurve = AnimationCurve.EaseInOut(0,0,1,1);
    private readonly List<Coroutine> _shootCos = new List<Coroutine>();

    public enum ORBIT_STATE
    {
        ORBIT_STANDBY,
        ORBIT_ONHEAD,
        ORBIT_SKY,
        ORBIT_SHOOT
    }
    private GameObject MainCamera;
    void Start()
    {
        player = transform.parent.gameObject.transform;
        MainCamera = Camera.main.gameObject;
        // 구체 미리 생성
        for (int i = 0; i < sphereCount; i++)
        {
            GameObject obj = Instantiate(spherePrefab, player.position, Quaternion.identity, gameObject.transform);
            spheres.Add(obj.transform);
        }
        SetOrbitState(ORBIT_STATE.ORBIT_STANDBY);
    }

    void Update()
    {
        switch (m_OrbitState)
        {
            case ORBIT_STATE.ORBIT_STANDBY:
                return;
            case ORBIT_STATE.ORBIT_ONHEAD:
            case ORBIT_STATE.ORBIT_SKY:
                ActiveTick();
                break;
            case ORBIT_STATE.ORBIT_SHOOT:
                // 발사는 코루틴 시퀀스가 처리(여기서는 아무 것도 하지 않음)
                break;
        }

    }

    void StandbyTick()
    {
        float time = Time.time * orbitSpeed;

        for (int i = 0; i < spheres.Count; i++)
        {
            // 각 구체가 배치될 목표 각도
            float angle = (360f / sphereCount) * i + time;
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 1.5f, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;

            Vector3 targetPos = player.position + offset;

            // 부드럽게 이동 (Lerp)
            spheres[i].position = targetPos;
            spheres[i].gameObject.SetActive(false);
        }

    }
    void ActiveTick()
    {
        float time = Time.time * orbitSpeed;

        for (int i = 0; i < spheres.Count; i++)
        {
            spheres[i].gameObject.SetActive(true);

            // 각 구체가 배치될 목표 각도
            float angle = (360f / sphereCount) * i + time;
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 1.5f, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            // ↑ Y=1.5f 로 하면 머리 위로 띄움. Y=0f 로 하면 바닥 주위 공전.

            Vector3 targetPos = player.position + offset;

            // 부드럽게 이동 (Lerp)
            spheres[i].position = Vector3.Lerp(spheres[i].position, targetPos, Time.deltaTime * lerpSpeed);
        }
    }
    void ShootTick()
    {
        // 사용 안 함(시퀀스로 대체). 필요 시 남겨둠.
    }

    private IEnumerator Co_ShootSequence()
    {
        // 모두 활성화
        foreach (var s in spheres) s.gameObject.SetActive(true);
        int iThrowCnt = 0; const int maxThrowCnt = 2; // 한 번에 발사할 구체 개수
        for (int i = 0; i < spheres.Count; i++)
        {
            if (iThrowCnt < maxThrowCnt)
            {
                ++iThrowCnt;
                yield return new WaitForSeconds(0.0f);
            }
            var sphere = spheres[i];
            // 카메라 주변 랜덤 오프셋(상하 흔들림은 과하지 않게 Y를 절반)
            Vector3 rand = Random.insideUnitSphere * shootTargetOffsetRadius;
            rand.y *= 0.5f;
            Vector3 target = MainCamera.transform.position+ (-MainCamera.transform.forward) + rand;

            var co = StartCoroutine(Co_ThrowTo(sphere, target, shootDuration));
            _shootCos.Add(co);
            ++iThrowCnt;
            // 다음 구체 발사까지 대기
            yield return new WaitForSeconds(0.0f);
        }
    }

    private IEnumerator Co_ThrowTo(Transform sphere, Vector3 target, float dur)
    {
        Vector3 start = sphere.position;
        dur = Mathf.Max(0.0001f, dur);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float e = shootCurve.Evaluate(k);
            sphere.position = Vector3.LerpUnclamped(start, target, e);
            yield return null;
        }
        sphere.position = target;
    }

    private void StopShootCoroutines()
    {
        if (_shootCos.Count == 0) return;
        foreach (var co in _shootCos) if (co != null) StopCoroutine(co);
        _shootCos.Clear();
    }

    public void SetOrbitState(ORBIT_STATE eState)
    {
        if (eState == ORBIT_STATE.ORBIT_STANDBY)
        {
            StopShootCoroutines();
            radius = 0.0f;

            StandbyTick();
        }
        else if (eState == ORBIT_STATE.ORBIT_ONHEAD)
        {
            StopShootCoroutines();
            radius = 0.9f;
        }
        else if (eState == ORBIT_STATE.ORBIT_SKY)
        {
            StopShootCoroutines();
            radius = 3.0f;
        }
        else if (eState == ORBIT_STATE.ORBIT_SHOOT)
        {
            StopShootCoroutines();
            StartCoroutine(Co_ShootSequence());
            // 공전 반경은 유지, 발사 코루틴이 위치를 덮어씀
            radius = radius <= 0f ? 0.9f : radius;
        }
        m_OrbitState = eState;
    }
    public void OrbitStart()
    {
        SetOrbitState(ORBIT_STATE.ORBIT_ONHEAD);
    }
    public void OrbitEnd()
    {
        SetOrbitState(ORBIT_STATE.ORBIT_STANDBY);
    }
    public void OrbitSky()
    {
        SetOrbitState(ORBIT_STATE.ORBIT_SKY);
    }
    public void OrbitShoot()
    {
        SetOrbitState(ORBIT_STATE.ORBIT_SHOOT);
    }

}

