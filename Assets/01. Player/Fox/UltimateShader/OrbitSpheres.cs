using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class OrbitSpheres : MonoBehaviour
{
    public GameObject spherePrefab;   // ������ ��ü ������
    private Transform player;          // �߽��� �Ǵ� �÷��̾�
    public int sphereCount = 20;      // ��ü ����
    public float radius = 3f;         // ���� �ݰ�
    public float orbitSpeed = 30f;    // ���� �ӵ� (deg/sec)
    public float lerpSpeed = 5f;      // �ε巴�� ������� ����

    private List<Transform> spheres = new List<Transform>();
    private ORBIT_STATE m_OrbitState = ORBIT_STATE.ORBIT_STANDBY;
    // �߻� ������ ����
    [Header("Shoot Settings")]
    [SerializeField] private float shootInterval = 0.001f;          // �� �� �������� ���� ��ü �߻�
    [SerializeField] private float shootDuration = 0.6f;            // ��ǥ �������� �ɸ��� �ð�
    [SerializeField] private float shootTargetOffsetRadius = 0.35f; // ī�޶� �ֺ� ���� ������ �ݰ�
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
        // ��ü �̸� ����
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
                // �߻�� �ڷ�ƾ �������� ó��(���⼭�� �ƹ� �͵� ���� ����)
                break;
        }

    }

    void StandbyTick()
    {
        float time = Time.time * orbitSpeed;

        for (int i = 0; i < spheres.Count; i++)
        {
            // �� ��ü�� ��ġ�� ��ǥ ����
            float angle = (360f / sphereCount) * i + time;
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 1.5f, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;

            Vector3 targetPos = player.position + offset;

            // �ε巴�� �̵� (Lerp)
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

            // �� ��ü�� ��ġ�� ��ǥ ����
            float angle = (360f / sphereCount) * i + time;
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 1.5f, Mathf.Sin(angle * Mathf.Deg2Rad)) * radius;
            // �� Y=1.5f �� �ϸ� �Ӹ� ���� ���. Y=0f �� �ϸ� �ٴ� ���� ����.

            Vector3 targetPos = player.position + offset;

            // �ε巴�� �̵� (Lerp)
            spheres[i].position = Vector3.Lerp(spheres[i].position, targetPos, Time.deltaTime * lerpSpeed);
        }
    }
    void ShootTick()
    {
        // ��� �� ��(�������� ��ü). �ʿ� �� ���ܵ�.
    }

    private IEnumerator Co_ShootSequence()
    {
        // ��� Ȱ��ȭ
        foreach (var s in spheres) s.gameObject.SetActive(true);
        int iThrowCnt = 0; const int maxThrowCnt = 2; // �� ���� �߻��� ��ü ����
        for (int i = 0; i < spheres.Count; i++)
        {
            if (iThrowCnt < maxThrowCnt)
            {
                ++iThrowCnt;
                yield return new WaitForSeconds(0.0f);
            }
            var sphere = spheres[i];
            // ī�޶� �ֺ� ���� ������(���� ��鸲�� ������ �ʰ� Y�� ����)
            Vector3 rand = Random.insideUnitSphere * shootTargetOffsetRadius;
            rand.y *= 0.5f;
            Vector3 target = MainCamera.transform.position+ (-MainCamera.transform.forward) + rand;

            var co = StartCoroutine(Co_ThrowTo(sphere, target, shootDuration));
            _shootCos.Add(co);
            ++iThrowCnt;
            // ���� ��ü �߻���� ���
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
            // ���� �ݰ��� ����, �߻� �ڷ�ƾ�� ��ġ�� ���
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

