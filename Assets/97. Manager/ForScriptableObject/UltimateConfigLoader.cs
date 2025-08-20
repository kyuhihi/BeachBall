using UnityEngine;

public static class UltimateConfigLoader
{
    // Resources ���� ���(Ȯ����/Assets/ ����)
    public const string FoxUltimatePath = "UltimateSetting/FoxUltimateSetting";
    public const string OriginEnvPath   = "OriginSkySetting";

    public static UltimateSetting LoadFoxUltimate()
        => Resources.Load<UltimateSetting>(FoxUltimatePath);

    public static EnvironmentConfig LoadOriginEnv()
        => Resources.Load<EnvironmentConfig>(OriginEnvPath);
}
