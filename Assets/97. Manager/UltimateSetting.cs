using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UltimateSetting", menuName = "Scriptable Objects/UltimateSetting")]
public class UltimateSetting : ScriptableObject
{
    [Serializable]
    public struct CutSceneTransform
    {
        [SerializeField] private IPlayerInfo.CourtPosition eCourtPosition;
        [SerializeField] private Vector3 cutScenePosition;
        [SerializeField] private Quaternion cutSceneRotation;

        public IPlayerInfo.CourtPosition GetCourtPosition() => eCourtPosition;
        public Vector3 GetCutScenePosition() => cutScenePosition;
        public Quaternion GetCutSceneRotation() => cutSceneRotation;
    }

    [SerializeField] private IPlayerInfo.PlayerType m_ePlayerType;

    [Header("CutScene Setting")]
    [SerializeField] private CutSceneTransform[] m_UltimatePositions; // ?/?

    [Header("Environment (Config Asset)")]
    [SerializeField] private EnvironmentConfig environment;


    private void OnValidate()
    {
        if (m_UltimatePositions == null || m_UltimatePositions.Length == 0)
            m_UltimatePositions = new CutSceneTransform[2];
    }

    public Vector3 GetUltimatePosition(IPlayerInfo.CourtPosition courtPosition)
    {
        if (m_UltimatePositions == null) return Vector3.zero;
        foreach (var t in m_UltimatePositions)
            if (t.GetCourtPosition() == courtPosition) return t.GetCutScenePosition();
        return Vector3.zero;
    }

    public Quaternion GetUltimateRotation(IPlayerInfo.CourtPosition courtPosition)
    {
        if (m_UltimatePositions == null) return Quaternion.identity;
        foreach (var t in m_UltimatePositions)
            if (t.GetCourtPosition() == courtPosition) return t.GetCutSceneRotation();
        return Quaternion.identity;
    }

    public Color ApplyEnvironment()//filter color is returned
    {
        if (environment == null) return Color.white;
        if (environment.SkyBoxMat != null) RenderSettings.skybox = environment.SkyBoxMat;
        return environment.LightFilterColor;
    }


}
