using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class FoxPlayerMovement : BasePlayerMovement
{
    [SerializeField] private Transform m_FireBallSpawnPoint;
    private FireBallContainer m_FireBallContainer;

    [Header("Ultimate Setting")]
    const string m_UltimateFlashGameObjName = "UltimateMesh";
    private GameObject m_UltimateFlashGameObject;
    private Material m_UltimateFlashMaterial;

    protected override void OnInterrupted()
    {
        base.OnInterrupted();
    }
    
    protected override void Start()
    {
        base.Start();
        m_PlayableDirector = GetComponent<PlayableDirector>();
        m_PlayerType = IPlayerInfo.PlayerType.Fox;
        m_PlayerDefaultColor = Color.orange;

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


        m_FireBallContainer = GameObject.FindFirstObjectByType<FireBallContainer>();
        int iChildCnt = transform.childCount;
        for (int i = 0; i < iChildCnt; i++)
        {
            if (transform.GetChild(i).name == m_UltimateFlashGameObjName)
            {
                m_UltimateFlashGameObject = transform.GetChild(i).gameObject;
                m_UltimateFlashMaterial = m_UltimateFlashGameObject.GetComponent<ParticleSystemRenderer>().material;
                break;
            }
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
            else if (track is CinemachineTrack)
            {
                m_PlayableDirector.SetGenericBinding(track, Camera.main.GetComponent<Cinemachine.CinemachineBrain>());
            }
        }
    }
    public override void OnAttackSkill(InputValue value)
    {
        if(m_isTitleScene)
        {
            Debug.Log("타이틀 씬에서는 배구만 하거라");
            // 타이틀 씬에서는 방어 스킬 사용 불가
            return;
        }

        if (!m_isMoveByInput || m_eLocomotionState == IdleWalkRunEnum.Swim || isUltimateSkillActiving)
        {
            return;
        }

        if (value.isPressed)
        {
            m_Animator.SetTrigger("AttackSkill");

        }
    }

    public void ShootFireBall()//state machine call
    {
        GameObject fireball = m_FireBallContainer.GetPooledFireBall(this.gameObject);
        fireball.GetComponent<FireBall>().ShootFireBall(m_FireBallSpawnPoint, this.gameObject);
                    if(m_CourtPosition == IPlayerInfo.CourtPosition.COURT_LEFT)
                GameSettings.Instance.AddLeftAttackSkillCount();
            else
                GameSettings.Instance.AddRightAttackSkillCount();
    }

    protected override void Update()
    {
        base.Update();
        if (m_UltimateFlashGameObject == null || m_UltimateFlashMaterial == null) return;
        m_UltimateFlashMaterial.SetFloat("_LocalYClipOffset", transform.position.y);
    }

    public override void OnDefenceSkill(InputValue value)
    {
        if(m_isTitleScene)
        {
            Debug.Log("타이틀 씬에서는 싸움만 하거라");
            // 타이틀 씬에서는 방어 스킬 사용 불가
            return;
        }

        if (!m_isMoveByInput && value.isPressed || m_eLocomotionState == IdleWalkRunEnum.Swim || isUltimateSkillActiving)
        {
            return;
        }

        m_Animator.SetBool("DefenceSkill", value.isPressed);


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

        Vector3 OutPos = Vector3.zero;
        Quaternion OutRot = Quaternion.identity;
        bool bRetVal = GameManager.GetInstance().GetUltimatePos(m_PlayerType, m_CourtPosition, out OutPos, out OutRot);

        if (bRetVal)
        {
            transform.position = OutPos;
            transform.rotation = OutRot;
            m_PlayableDirector.Play();
        }
    }
    public override void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        m_isMoveByInput = false;
        muteFootSfx = true;
        footstepTimer = 0f;

        if (playerType == m_PlayerType && courtPosition == m_CourtPosition)
            m_UltimateFlashGameObject.SetActive(true);
        
        isUltimateSkillActiving = true;

    }
    public override void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (!this || gameObject == null || !isActiveAndEnabled) return;

        muteFootSfx = false;
        footstepTimer = 0f;

        if (playerType == m_PlayerType && courtPosition == m_CourtPosition)
            m_UltimateFlashGameObject.SetActive(false);

        if (HurtTurtleUltimateSkillStun)
        {
            HurtTurtleUltimateSkillStun = false;
        }
        else
        {
            m_isMoveByInput = true;
        }
        isUltimateSkillActiving = false;
    }//이거 오버라이딩해야함.
    public override void OnRoundStart()
    {
        StopAllCoroutines();
        m_isMoveByInput = true;
        SetResetMode(); SetTransformToRoundStart();


    }
    public override void OnRoundEnd()
    {
        StopAllCoroutines();
        SetTransformToRoundStart();
        SetResetMode();

        m_isMoveByInput = false;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Animator.SetBool("DefenceSkill", false);

    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
    }

}
