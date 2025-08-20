using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentConfig", menuName = "Scriptable Objects/EnvironmentConfig")]
public class EnvironmentConfig : ScriptableObject
{
    public Material SkyBoxMat;
    public Color LightFilterColor = Color.white;
}
