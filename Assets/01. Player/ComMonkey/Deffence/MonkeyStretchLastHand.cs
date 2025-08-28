using UnityEngine;

public class MonkeyStretchLastHand : MonoBehaviour
{
    [SerializeField] GameObject _CloudParticlePrefab;
    GameObject _CloudParticleInstance;
    void Start()
    {
        //_CloudParticleInstance = Instantiate(_CloudParticlePrefab, transform.position, Quaternion.identity);
    }

    void OnEnable()
    {
        if (_CloudParticleInstance)
        {
            _CloudParticleInstance.transform.position = gameObject.transform.position;
            Vector3 Rot = gameObject.transform.rotation.eulerAngles;
            Rot.x = 0;
            Rot.y += 180f;
            Rot.z = 0;
            
            _CloudParticleInstance.transform.localRotation = Quaternion.Euler(Rot);  

            _CloudParticleInstance.SetActive(true);
        }
        else
        {
            _CloudParticleInstance = Instantiate(_CloudParticlePrefab, transform.position, Quaternion.identity);
        }
    }

    void OnDisable()
    {
        if (_CloudParticleInstance)
            _CloudParticleInstance.SetActive(false);

    }


}
