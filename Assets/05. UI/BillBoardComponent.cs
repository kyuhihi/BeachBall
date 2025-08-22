using UnityEngine;

[ExecuteAlways]
public class BillBoardComponent : MonoBehaviour
{
    public enum BillboardMode
    {
        Full,       // ī�޶� ������ �ٶ� (LookAt)
        YAxisOnly   // ����(Y) ȸ���� ���� (����鿡���� ȸ��)
    }

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoFindMainCamera = true;

    [Header("Billboard")]
    [SerializeField] private BillboardMode mode = BillboardMode.Full;
    [SerializeField] private bool autoBillboard = true;
    [SerializeField] private bool faceCameraBackwards = false; // �ʿ�� ������(2D ��������Ʈ ������ ���)

    [Header("Debug")]
    [SerializeField] private bool showMissingCameraWarning = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // �����Ϳ��� �� �ٲ�� ��� �ݿ�
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

        // ī�޶� �������� ȸ��
        Vector3 toCamera = targetCamera.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(toCamera.normalized);
        

        // Y�� ȸ���� ���� ����� ���
        if (mode == BillboardMode.YAxisOnly)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }

        // ī�޶� ������� �� ���
        if (faceCameraBackwards)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.y += 180;
            transform.rotation = Quaternion.Euler(euler);
        }
    }

    private void TryFindMainCamera()
    {
        // �±װ� "MainCamera"�� ī�޶� ã��
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
