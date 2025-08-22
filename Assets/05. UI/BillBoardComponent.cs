using UnityEngine;

[ExecuteAlways]
public class BillBoardComponent : MonoBehaviour
{
    public enum BillboardMode
    {
        Full,       // 카메라를 완전히 바라봄 (LookAt)
        YAxisOnly   // 수직(Y) 회전만 적용 (수평면에서만 회전)
    }

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoFindMainCamera = true;

    [Header("Billboard")]
    [SerializeField] private BillboardMode mode = BillboardMode.Full;
    [SerializeField] private bool autoBillboard = true;
    [SerializeField] private bool faceCameraBackwards = false; // 필요시 뒤집기(2D 스프라이트 뒤집힌 경우)

    [Header("Debug")]
    [SerializeField] private bool showMissingCameraWarning = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 값 바뀌면 즉시 반영
        if (autoFindMainCamera && (targetCamera == null))
            TryFindMainCamera();
        if (autoBillboard)
            DoBillboard();
    }
#endif

    private void Awake()
    {
        if (autoFindMainCamera && targetCamera == null)
            TryFindMainCamera();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (autoBillboard)
            DoBillboard();
    }

    private void DoBillboard()
    {
        if (targetCamera == null)
            return;

        // 카메라 방향으로 회전
        Vector3 toCamera = targetCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(toCamera.normalized);
        

        // Y축 회전만 적용 모드일 경우
        if (mode == BillboardMode.YAxisOnly)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }

        // 카메라를 뒤집어야 할 경우
        if (faceCameraBackwards)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.y += 180;
            transform.rotation = Quaternion.Euler(euler);
        }
    }

    private void TryFindMainCamera()
    {
        // 태그가 "MainCamera"인 카메라 찾기
        GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObject != null)
        {
            targetCamera = cameraObject.GetComponent<Camera>();
            if (showMissingCameraWarning)
                Debug.Log("Main camera found: " + cameraObject.name, this);
        }
        else if (showMissingCameraWarning)
        {
            Debug.LogWarning("Main camera not found. Please assign a camera to the BillBoardComponent.", this);
        }
    }
}
