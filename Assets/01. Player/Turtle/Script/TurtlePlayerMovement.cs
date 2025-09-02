using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class TurtlePlayerMovement : BasePlayerMovement
{
    [SerializeField] private ParticleSystem waterCannonParticlePrefab;
    private ParticleSystem waterCannonParticleInstance;

    [SerializeField] private ParticleSystem waterCannonByEffectParticlePrefab;
    private ParticleSystem waterCannonByEffectParticleInstance;



    [SerializeField] private ParticleSystem dropWaterFromWallPrefab;
    private ParticleSystem dropWaterFromWallInstance;

    private Vector3 dropwaterFromWallPos = Vector3.zero;
    private Quaternion dropwaterFromWallRot = Quaternion.identity;

    [SerializeField] private ParticleSystem waterDropParticlePrefab;
    private ParticleSystem waterDropParticleInstance;

    private Vector3 waterDropParticlePos = Vector3.zero;
    private Quaternion waterDropParticleRot = Quaternion.identity;

    [SerializeField] private GameObject ultimateWaterPrefab;
    private GameObject ultimateWaterInstance;

    private Vector3 ultimateWaterPos = Vector3.zero;
    private Quaternion ultimateWaterRot = Quaternion.identity;


    [SerializeField] private GameObject waterDragonPrefab;
    private GameObject waterDragonInstance;

    private Vector3 waterDragonPos = Vector3.zero;
    private Quaternion waterDragonRot = Quaternion.identity;


    private Vector3 waterDragonMoveBeforeAttack = new Vector3(0, 0, 15f); // 드래곤이 공격 전 위치를 이동하기 위한




    [SerializeField] private ParticleSystem waterDragonSplashPrefab;
    private ParticleSystem waterDragonSplashInstance;

    private Vector3 waterDragonSplashPos = Vector3.zero;
    private Quaternion waterDragonSplashRot = Quaternion.identity;

    [SerializeField] private ParticleSystem waterTornadoPrefab;
    private ParticleSystem waterTornadoInstance;

    private Vector3 waterTornadoPos = Vector3.zero;
    private Quaternion waterTornadoRot = Quaternion.identity;

    [SerializeField] private ParticleSystem ultimateEffectPrefab;
    private ParticleSystem ultimateEffectInstance;



    [SerializeField] private Transform mouthTransform;

    // 거북이 attack skill
    private bool isShellThrowCannonActive = false;

    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform shellHoldPoint; // 손에 들고 있을 위치
    [SerializeField] private Transform shellThrowPoint; // 던질 때 시작 위치
    [SerializeField] private GameObject throwEffectPrefab; // 이펙트 프리팹
    private GameObject heldShell = null;


    // 물고기 프리팹(ultimatewater 안에 생성)
    [SerializeField] private GameObject fishPrefab1;
    [SerializeField] private GameObject fishPrefab2;
    [SerializeField] private GameObject fishPrefab3;

    // 각 종류별로 리스트로 관리
    private List<GameObject> fishList1 = new List<GameObject>();
    private List<GameObject> fishList2 = new List<GameObject>();
    private List<GameObject> fishList3 = new List<GameObject>();

    private int fishCount = 4; // 각 종류별로 생성할 물고기 개수

    private Vector3 ballPos = Vector3.zero;

    private bool isWaterCannonActive = false;
    private bool isWaterCannonRotating = false;
    private bool isUltimateSkillActiving = false;
    private float waterCannonTurnSpeed = 180f; // 초당 회전 각도
    private float waterCannonAngleThreshold = 5f; // 몇 도 이내면 "완료"로 간주

    private bool _autoThrownOnInterrupt = false;



    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();

        var timeline = m_PlayableDirector.playableAsset as TimelineAsset;
        if (timeline == null) return;
        m_PlayerType = IPlayerInfo.PlayerType.Turtle;
        m_PlayerDefaultColor = Color.skyBlue;

        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (active != "TitleScene")
        {
            PlayerUIManager.GetInstance().SetPlayerInfoInUI(this);
            SetEmptyPlayableDirector();
        }
        else
        {
            m_isTitleScene = true;
        }

        if (m_CourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
        {
            dropwaterFromWallPos = new Vector3(0.3f, 6.28f, -11.56f);
            dropwaterFromWallRot = Quaternion.Euler(21.364996f, 0, 0);

            waterDropParticlePos = new Vector3(-0.019f, 0.202f, -10.842f);
            waterDropParticleRot = Quaternion.identity;

            waterDragonPos = new Vector3(0.326f, 5.809f, -17.979f);
            waterDragonRot = Quaternion.Euler(0, -90, 0f);

            waterDragonSplashPos = new Vector3(0f, 0.2f, 5.5f);
            waterDragonSplashRot = Quaternion.identity;

            waterTornadoPos = new Vector3(0f, 0.2f, 5.5f);
            waterTornadoRot = Quaternion.identity;

            ultimateWaterPos = new Vector3(0f, -4f, 0f);
            ultimateWaterRot = Quaternion.identity;

        }
        else // COURT_LEFT
        {
            dropwaterFromWallPos = new Vector3(-0.3f, 6.28f, 11.56f); // 예시값, 실제 위치에 맞게 조정
            dropwaterFromWallRot = Quaternion.Euler(21.364996f, 180f, 0);

            waterDropParticlePos = new Vector3(0.019f, 0.202f, 10.842f);
            waterDropParticleRot = Quaternion.identity;

            waterDragonPos = new Vector3(-0.326f, 5.809f, 17.979f);
            waterDragonRot = Quaternion.Euler(0, 90, 0f);

            waterDragonSplashPos = new Vector3(0f, 0.2f, -5.5f);
            waterDragonSplashRot = Quaternion.identity;

            waterTornadoPos = new Vector3(0f, 0.2f, -5.5f);
            waterTornadoRot = Quaternion.identity;

            ultimateWaterPos = new Vector3(0f, -4f, 0f);
            ultimateWaterRot = Quaternion.identity;
        }

    }

    private void SetEmptyPlayableDirector()
    {
        var timeline = m_PlayableDirector.playableAsset as TimelineAsset;
        if (timeline == null) return;

        foreach (var track in timeline.GetOutputTracks())
        {
            // 이미 바인딩된 경우는 건너뜀
            var currentBinding = m_PlayableDirector.GetGenericBinding(track);
            if (currentBinding != null)
                continue;

            string groupName = track.parent != null ? track.parent.name : "";

            if (groupName == "Camera" && track is AnimationTrack)
            {
                GameObject cutSceneCamera = GameManager.GetInstance().GetCutSceneCamera();
                if (cutSceneCamera == null)
                    GameManager.GetInstance().Start();
                cutSceneCamera = GameManager.GetInstance().GetCutSceneCamera();
                m_PlayableDirector.SetGenericBinding(track, cutSceneCamera.GetComponent<Animator>());
                
                
            }
            else if (track is UnityEngine.Timeline.SignalTrack)
            {
                var gm = FindFirstObjectByType<GameManager>();
                if (gm != null)
                    m_PlayableDirector.SetGenericBinding(track, gm);
            }
        }
    }

    public override void OnAttackSkill(InputValue value)
    {
        if (!m_isMoveByInput || m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }
        if (value.isPressed)
        {
            if (isShellThrowCannonActive || isWaterCannonActive || isWaterCannonRotating || isUltimateSkillActiving)
                return;
            isShellThrowCannonActive = true;
            // Debug.Log("Turtle: 등껍질 돌진!");
            _autoThrownOnInterrupt = false;
            MoveByInput = false;

            // 등껍질 미리 생성해서 손에 들고 있게
            if (heldShell == null && shellPrefab != null && shellHoldPoint != null)
            {
                heldShell = Instantiate(shellPrefab, shellHoldPoint.position, shellHoldPoint.rotation, shellHoldPoint);

                GameObject throweffect = Instantiate(throwEffectPrefab, shellHoldPoint.position, shellHoldPoint.rotation);
                // throweffect.transform.SetParent(shellHoldPoint); // 이 줄을 제거!
                Destroy(throweffect, 1f); // 1초 뒤 자동 파괴 (필요시 시간 조절) -> 어차피 알아서 없어짐

            }

            // 애니메이션 트리거
            if (m_Animator != null)
                m_Animator.SetTrigger("ThrowShell");
        }
    }

    public override void OnDefenceSkill(InputValue value)
    {
        if(m_isTitleScene)
        {
            Debug.Log("타이틀 씬에서는 싸움만 하거라");
            // 타이틀 씬에서는 방어 스킬 사용 불가
            return;
        }

        if (!m_isMoveByInput || m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }

        if (value.isPressed)
        {
            // 이미 물대포가 진행 중이거나 회전 중이면 무시
            if (isWaterCannonActive || isWaterCannonRotating || isShellThrowCannonActive || isUltimateSkillActiving)
                return;

            isWaterCannonRotating = true;
            MoveByInput = false;
        }
    }
    public override void OnUltimateSkill(InputValue value)
    {
        if(m_isTitleScene)
        {
            Debug.Log("타이틀 씬에서는 싸움만 하거라");
            // 타이틀 씬에서는 궁극기 스킬 사용 불가
            return;
        }


        if (!m_isMoveByInput || m_eLocomotionState == IdleWalkRunEnum.Swim)
        {
            return;
        }
        if (!PlayerUIManager.GetInstance().UseAbility(IUIInfo.UIType.UltimateBar, m_CourtPosition))
        {
            return;
        }

        if (value.isPressed)
        {

            if (isWaterCannonActive || isWaterCannonRotating || isShellThrowCannonActive || isUltimateSkillActiving)
                return;

            Vector3 OutPos = Vector3.zero;
            Quaternion OutRot = Quaternion.identity;
            bool bRetVal = GameManager.GetInstance().GetUltimatePos(m_PlayerType, m_CourtPosition, out OutPos, out OutRot);

            // 1. Player 태그 가진 모든 오브젝트 찾기
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in players)
            {
                // 2. 나 자신이 아니면
                if (player != this.gameObject)
                {
                    var controller = player.GetComponent<BasePlayerMovement>();
                    if (controller != null)
                    {

                        controller.transform.position = waterTornadoPos;
                        controller.transform.rotation = waterTornadoRot;
                    }
                }
            }

            if (bRetVal)
            {
                transform.position = OutPos;
                transform.rotation = OutRot;
                m_PlayableDirector.Play();

            }
        }
    }
    public void SummonDropWaterFromWall()
    {
        if (dropWaterFromWallPrefab != null && dropWaterFromWallInstance == null)
        {
            // dropWaterFromWallInstance = Instantiate(dropWaterFromWallPrefab, transform.position, Quaternion.identity);
            dropWaterFromWallInstance = Instantiate(dropWaterFromWallPrefab, dropwaterFromWallPos, dropwaterFromWallRot);

            // Debug.Log(dropwaterFromWallPos + " " + dropwaterFromWallRot);

            // dropWaterFromWallInstance.transform.SetParent(transform);
            // dropWaterFromWallInstance.transform.localPosition = Vector3.zero;
            dropWaterFromWallInstance.Play();
        }
        
    }
    
    public void SummonWaterDropParticle()
    {
        if (waterDropParticlePrefab != null && waterDropParticleInstance == null)
        {
            waterDropParticleInstance = Instantiate(waterDropParticlePrefab, waterDropParticlePos, waterDropParticleRot);
            // waterDropParticleInstance.transform.SetParent(transform);
            // waterDropParticleInstance.transform.localPosition = Vector3.zero;

            waterDropParticleInstance.Play();
        }
    }

    public void SummonUltimateWater()
    {
        if (ultimateWaterPrefab != null && ultimateWaterInstance == null)
        {
            ultimateWaterInstance = Instantiate(ultimateWaterPrefab, ultimateWaterPos, ultimateWaterRot);
            var ultimateWater = ultimateWaterInstance.GetComponent<UltimateWater>();
            if (ultimateWater != null)
            {
                ultimateWater.StartFillWater();
            }
        }
    }

    public void UltimateWaterSpeedUp()
    {
        if (ultimateWaterInstance != null)
        {
            var ultimateWater = ultimateWaterInstance.GetComponent<UltimateWater>();
            if (ultimateWater != null)
            {
                ultimateWater.SpeedUpFill();
            }

        }
        
    }



    public void SummonWaterDragon()
    {
        if (waterDragonPrefab != null && waterDragonInstance == null)
        {
            waterDragonInstance = Instantiate(waterDragonPrefab, waterDragonPos, waterDragonRot);
            // waterDragonInstance.transform.SetParent(transform);
            // waterDragonInstance.transform.localPosition = Vector3.zero;

            // waterDragonInstance.Play();

            // Dragon 컴포넌트에 접근해서 MoveBeforeUltimateAttack 값 할당
            var dragon = waterDragonInstance.GetComponent<Dragon>();
            if (dragon != null)
            {
                // 오른쪽/왼쪽 코트에 따라 방향 다르게
                dragon.MoveBeforeUltimateAttack = (m_CourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT) ? waterDragonMoveBeforeAttack : -waterDragonMoveBeforeAttack;
            }
        }
    }

    public void StartDrillDragon()
    {
        if (waterDragonInstance != null)
        {
            var dragon = waterDragonInstance.GetComponent<Dragon>();
            if (dragon != null)
            {
                dragon.DragonRotate = waterDragonRot; // 현재 회전값 저장
                dragon.StartDrill(this.gameObject); // drill 시작
            }
        }
    }


    public void SummonSplashAndTornado()
    {
        if (waterDragonSplashPrefab != null && waterDragonSplashInstance == null)
        {
            waterDragonSplashInstance = Instantiate(waterDragonSplashPrefab, waterDragonSplashPos, waterDragonSplashRot);
            waterDragonSplashInstance.Play();
        }

        if (waterTornadoPrefab != null && waterTornadoInstance == null)
        {
            waterTornadoInstance = Instantiate(waterTornadoPrefab, waterTornadoPos, waterTornadoRot);
            waterTornadoInstance.Play();
        }

        // ScreenWaterPlane 오브젝트를 찾고 ExecuteWaterSplash() 호출
        var screenWater = FindFirstObjectByType<ScreenWaterPlane>();
        if (screenWater != null)
        {
            screenWater.ExecuteWaterSplash();
        }
    }

    public void StopAndDestroySplashAndTornado()
    {
        if (waterDragonSplashInstance != null)
        {
            var main = waterDragonSplashInstance.main;
            main.loop = false;
            waterDragonSplashInstance.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(DestroyParticleWhenDone(waterDragonSplashInstance));
            waterDragonSplashInstance = null;
        }
        if (waterTornadoInstance != null)
        {
            var main = waterTornadoInstance.main;
            main.loop = false;
            waterTornadoInstance.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            StartCoroutine(DestroyParticleWhenDone(waterTornadoInstance));
            waterTornadoInstance = null;
        }
        
    }

    private IEnumerator DestroyParticleWhenDone(ParticleSystem ps)
    {
        // 파티클이 모두 끝날 때까지 대기
        while (ps != null && ps.IsAlive(true))
            yield return null;

        if (ps != null)
            Destroy(ps.gameObject);
    }

    public void SummonUltimateEffect()
    {
        if (ultimateEffectPrefab != null && ultimateEffectInstance == null)
        {
            ultimateEffectInstance = Instantiate(ultimateEffectPrefab, transform.position, Quaternion.identity);
            ultimateEffectInstance.transform.SetParent(transform);
            ultimateEffectInstance.transform.localPosition = Vector3.zero;
        }
    }

    public void DestroyAllEffectAndSummonFish()
    {
        if (dropWaterFromWallInstance)
            Destroy(dropWaterFromWallInstance);

        if (waterDropParticleInstance)
            Destroy(waterDropParticleInstance);

        if (waterDragonInstance)
            Destroy(waterDragonInstance);

        if (ultimateEffectInstance)
            Destroy(ultimateEffectInstance);

        // UltimateWater 영역 정보 가져오기
        if (ultimateWaterInstance != null)
        {
            Transform waterTr = ultimateWaterInstance.transform;
            Vector3 scale = waterTr.localScale;

            // 큐브 Mesh의 기본 크기가 1x1x1이므로, -0.5~+0.5에 스케일 곱하기
            Vector3 areaMin = new Vector3(-0.5f * scale.x, 0f, -0.5f * scale.z);
            Vector3 areaMax = new Vector3(0.5f * scale.x, scale.y, 0.5f * scale.z);

            GameObject[] fishPrefabs = { fishPrefab1, fishPrefab2, fishPrefab3 };
            List<GameObject>[] fishLists = { fishList1, fishList2, fishList3 };

            for (int j = 0; j < fishPrefabs.Length; j++)
            {
                for (int i = 0; i < fishCount; i++)
                {
                    Vector3 localPos = new Vector3(
                        Random.Range(areaMin.x, areaMax.x),
                        Random.Range(areaMin.y, areaMax.y - 2f),
                        Random.Range(areaMin.z, areaMax.z)
                    );

                    GameObject fish = Instantiate(fishPrefabs[j], localPos, Quaternion.identity);
                    fishLists[j].Add(fish);
                    if (j == 0)
                    {
                        var fishCtrl = fish.GetComponent<FishController>();
                        if (fishCtrl != null)
                            fishCtrl.Init(waterTr, areaMin, areaMax, 2f, 200f);
                    }
                    else
                    {
                        var fishCtrl = fish.GetComponent<FishController>();
                        if (fishCtrl != null)
                            fishCtrl.Init(waterTr, areaMin, areaMax, 2f, 0f);
                    }

                }
            }
        }
    }

    public override void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        MoveByInput = false;
        if (courtPosition != m_CourtPosition)
        {
            return;
        }
        isUltimateSkillActiving = true;
    }

    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (!this || gameObject == null || !isActiveAndEnabled) return;

        if (HurtTurtleUltimateSkillStun)
        {
            HurtTurtleUltimateSkillStun = false;
        }
        else
        {
            MoveByInput = true;
        }
        

        if (courtPosition != m_CourtPosition)
        {
            return;
        }

        isUltimateSkillActiving = false;

        // 여기서 코드포지션이 같아야만 실행하도록 하기

        // 1. Player 태그 가진 모든 오브젝트 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            // 2. 나 자신이 아니면
            if (player != this.gameObject)
            {
                var controller = player.GetComponent<BasePlayerMovement>();
                if (controller != null)
                {

                    controller.Stun(10f); // 10초간 스턴
                    controller.IsSwimming = true;
                    controller.HurtTurtleUltimateSkillStun = true;
                    controller.SetSwimModeAfterStun(0.5f); // 10초 후 수영모드, 속도 0.5배
                }
            }
            else
            {
                IsSwimming = true;
                SetSwimmingMode(2f); // 수영모드, 속도 2배
            }
            
            var rb = player.GetComponent<Rigidbody>();
                    if (rb != null)
                        rb.useGravity = false;
        }
    }
    

    private void SafeDestroyPS(ref ParticleSystem ps)
    {
        if (ps)
        {
            Destroy(ps.gameObject);
            ps = null;
        }
    }
    private void SafeDestroyGO(ref GameObject go)
    {
        if (go)
        {
            Destroy(go);
            go = null;
        }
    }

    public override void OnRoundStart()
    {
        Debug.Log("Turtle OnRoundStart");
        // 코루틴/트리거/상태 초기화
        StopAllCoroutines();

        SetTransformToRoundStart();
        m_isMoveByInput = true;

        isWaterCannonActive = false;
        isWaterCannonRotating = false;
        isShellThrowCannonActive = false;
        isUltimateSkillActiving = false;
        HurtTurtleUltimateSkillStun = false;
        _autoThrownOnInterrupt = false;
        SetResetMode();

        if (m_Animator != null)
        {
            m_Animator.ResetTrigger("ThrowShell");
            m_Animator.ResetTrigger("WaterCannon");
            m_Animator.SetBool("WaterCannonActive", false);
        }

        // 남아있던 셸 제거
        if (heldShell && heldShell.transform) heldShell.transform.SetParent(null);
        SafeDestroyGO(ref heldShell);

        // 입 방향 초기화
        if (mouthTransform) mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);

  
        var selfRb = GetComponent<Rigidbody>();
        if (selfRb) selfRb.useGravity = true;

        // 남은 이펙트 강제 정리(방어적)
        SafeDestroyPS(ref waterCannonParticleInstance);
        SafeDestroyPS(ref waterCannonByEffectParticleInstance);
        SafeDestroyPS(ref dropWaterFromWallInstance);
        SafeDestroyPS(ref waterDropParticleInstance);
        SafeDestroyGO(ref ultimateWaterInstance);
        SafeDestroyGO(ref waterDragonInstance);
        SafeDestroyPS(ref waterDragonSplashInstance);
        SafeDestroyPS(ref waterTornadoInstance);
        SafeDestroyPS(ref ultimateEffectInstance);

        DestroyAllFishes();
    }

    public override void OnRoundEnd()
    {
        SetTransformToRoundStart();

        Debug.Log("Turtle OnRoundEnd");
        // 더 이상 입력/코루틴 진행 금지
        m_isMoveByInput = false;
        StopAllCoroutines();

        // 애니메이터 상태/트리거 정리
        if (m_Animator != null)
        {
            m_Animator.ResetTrigger("ThrowShell");
            m_Animator.ResetTrigger("WaterCannon");
            m_Animator.SetBool("WaterCannonActive", false);
        }

        // 상태값 초기화
        isWaterCannonActive = false;
        isWaterCannonRotating = false;
        isShellThrowCannonActive = false;
        isUltimateSkillActiving = false;
        _autoThrownOnInterrupt = false;

        // 물리 정지
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_eLocomotionState = IdleWalkRunEnum.Idle;

        // 진행 중 컷신 중지
        if (m_PlayableDirector) m_PlayableDirector.Stop();

        // 셸 제거
        if (heldShell && heldShell.transform) heldShell.transform.SetParent(null);
        SafeDestroyGO(ref heldShell);

        // 이펙트/프리팹 전부 정리
        SafeDestroyPS(ref waterCannonParticleInstance);
        SafeDestroyPS(ref waterCannonByEffectParticleInstance);
        SafeDestroyPS(ref dropWaterFromWallInstance);
        SafeDestroyPS(ref waterDropParticleInstance);
        SafeDestroyGO(ref ultimateWaterInstance);
        SafeDestroyGO(ref waterDragonInstance);
        SafeDestroyPS(ref waterDragonSplashInstance);
        SafeDestroyPS(ref waterTornadoInstance);
        SafeDestroyPS(ref ultimateEffectInstance);

        // 물고기 삭제
        DestroyAllFishes();

        // 입 방향 리셋
        if (mouthTransform) mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);

        SetResetMode();
    }

    private void DestroyAndClearFishList(List<GameObject> list)
    {
        if (list == null) return;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var go = list[i];
            if (go) Destroy(go);
        }
        list.Clear();
    }

    private void DestroyAllFishes()
    {
        DestroyAndClearFishList(fishList1);
        DestroyAndClearFishList(fishList2);
        DestroyAndClearFishList(fishList3);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            PlayerUIManager.GetInstance().UpUltimateBar(m_CourtPosition, 0.1f);
        }

        GameObject ballObj = GameObject.FindWithTag(ballTag);

        // 스턴으로 애니메이션 이벤트가 끊겨도 즉시 던지기
        if (isShellThrowCannonActive && heldShell != null && isStunned && !_autoThrownOnInterrupt)
        {
            _autoThrownOnInterrupt = true;
            ThrowShellAtOpponent(); // 애니메이션 이벤트 없이 강제 실행
        }

        // 1. 물대포 회전 중: 몸통/머리 서서히 Ball 쪽으로 회전
        if (isWaterCannonRotating && ballObj != null && mouthTransform != null)
        {
            // 몸통(Y축만)
            Vector3 dir = (ballObj.transform.position - transform.position).normalized;
            Vector3 lookDir = new Vector3(dir.x, 0f, dir.z);
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                float angle = Quaternion.Angle(transform.rotation, Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f));
                float step = waterCannonTurnSpeed * Time.fixedDeltaTime;
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f),
                    step
                );

                // 머리(입)
                Vector3 mouthDir = (ballObj.transform.position - mouthTransform.position).normalized;
                Quaternion mouthTargetRot = Quaternion.LookRotation(mouthDir);
                mouthTransform.rotation = Quaternion.RotateTowards(
                    mouthTransform.rotation,
                    mouthTargetRot,
                    step
                );

                // 일정 각도 이내면 애니메이션 트리거
                if (angle < waterCannonAngleThreshold)
                {
                    isWaterCannonRotating = false;
                    isWaterCannonActive = true;
                    if (m_Animator != null)
                        m_Animator.SetTrigger("WaterCannon");
                    m_Animator.SetBool("WaterCannonActive", true);
                }
            }
        }

        // 2. 파티클이 나가는 동안에는 계속 Ball을 따라 회전
        if (waterCannonParticleInstance != null && waterCannonParticleInstance.isPlaying)
        {
            if (ballObj != null && mouthTransform != null)
            {
                Vector3 dir = (ballObj.transform.position - transform.position).normalized;
                Vector3 lookDir = new Vector3(dir.x, 0f, dir.z);
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                    float step = waterCannonTurnSpeed * Time.fixedDeltaTime;
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.Euler(0f, targetRot.eulerAngles.y, 0f),
                        step
                    );
                }

                Vector3 mouthDir = (ballObj.transform.position - mouthTransform.position).normalized;
                Quaternion mouthTargetRot = Quaternion.LookRotation(mouthDir);
                float mouthStep = waterCannonTurnSpeed * Time.fixedDeltaTime;
                mouthTransform.rotation = Quaternion.RotateTowards(
                    mouthTransform.rotation,
                    mouthTargetRot,
                    mouthStep
                );

                waterCannonParticleInstance.transform.position = mouthTransform.position;
                waterCannonParticleInstance.transform.rotation = mouthTransform.rotation;

                if (waterCannonByEffectParticleInstance != null)
                {
                    waterCannonByEffectParticleInstance.transform.position = mouthTransform.position;
                    waterCannonByEffectParticleInstance.transform.rotation = mouthTransform.rotation;
                }
            }
        }
    }

    public void ThrowShellAtOpponent()
    {
        // Debug.Log("Turtle: 등껍질 던지기!!!!!!");
        if (heldShell == null)
        {
            // Debug.LogWarning("손에 든 등껍질이 없습니다!");
            return;
        }

        // 상대 플레이어 찾기
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        GameObject opponent = null;
        foreach (var player in players)
        {
            if (player != this.gameObject)
            {
                opponent = player;
                break;
            }
        }
        if (opponent == null)
        {
            Debug.LogWarning("상대 플레이어를 찾을 수 없습니다!");
            Destroy(heldShell);
            heldShell = null;
            return;
        }

        // 등껍질을 손에서 떼고 던지기 위치로 이동
        heldShell.transform.SetParent(null);
        if (shellThrowPoint != null)
            heldShell.transform.position = shellThrowPoint.position;

        // 던질 방향 계산
        Vector3 throwDir = (opponent.transform.position - heldShell.transform.position).normalized;

        // Rigidbody로 힘을 가해 던지기
        Rigidbody shellRb = heldShell.GetComponent<Rigidbody>();
        if (shellRb != null)
        {
            float throwForce = 20f; // 원하는 힘으로 조절
            shellRb.isKinematic = false; // 혹시 손에 들 때 kinematic으로 했다면 해제
            shellRb.AddForce(throwDir * throwForce, ForceMode.Impulse);
        }

        MoveByInput = true; // 던진 후 이동 가능
        heldShell = null; // 손에 든 등껍질 비움
        isShellThrowCannonActive = false; // 등껍질 던지기 종료
    }

    // 애니메이션 이벤트에서 호출
    public void FireWaterCannon()
    {
        // Debug.Log("Turtle: 물대포 발사!");
        GameObject ballObj = GameObject.FindWithTag(ballTag);
        if (ballObj != null && waterCannonParticlePrefab != null && mouthTransform != null)
        {
            ballPos = ballObj.transform.position;
            Vector3 dir = (ballPos - mouthTransform.position).normalized;

            mouthTransform.rotation = Quaternion.LookRotation(dir);

            waterCannonParticleInstance = Instantiate(
                waterCannonParticlePrefab,
                mouthTransform.position,
                mouthTransform.rotation
            );
            waterCannonParticleInstance.Play();

            if (waterCannonByEffectParticlePrefab != null)
            {
                waterCannonByEffectParticleInstance = Instantiate(
                    waterCannonByEffectParticlePrefab,
                    mouthTransform.position,
                    mouthTransform.rotation
                );
                waterCannonByEffectParticleInstance.Play();
            }

            StartCoroutine(EnableMoveAfterParticle(waterCannonParticleInstance.main.duration));
            StartCoroutine(RestoreMouthRotationAfterDelay(waterCannonParticleInstance.main.duration));
        }
    }

    private IEnumerator EnableMoveAfterParticle(float delay)
    {
        yield return new WaitForSeconds(delay);
        mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);
    }

    private IEnumerator RestoreMouthRotationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 파티클 인스턴스 삭제
        if (waterCannonParticleInstance != null)
        {
            Destroy(waterCannonParticleInstance.gameObject);
            waterCannonParticleInstance = null;
        }
        if (waterCannonByEffectParticleInstance != null)
        {
            Destroy(waterCannonByEffectParticleInstance.gameObject);
            waterCannonByEffectParticleInstance = null;
        }

        isWaterCannonActive = false; // 물대포 끝남
        MoveByInput = true;
        if (m_Animator != null)
            m_Animator.SetBool("WaterCannonActive", false);
        mouthTransform.localRotation = Quaternion.Euler(180f, 90f, 0f);
    }

}