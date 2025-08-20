using UnityEngine;

public static class UltimateConfigLoader
{
    // Resources 기준 경로(확장자/Assets/ 제외)
    public const string FoxUltimatePath = "UltimateSetting/FoxUltimateSetting";
    public const string OriginEnvPath   = "OriginSkySetting";

    public static UltimateSetting LoadFoxUltimate()
        => Resources.Load<UltimateSetting>(FoxUltimatePath);

    public static EnvironmentConfig LoadOriginEnv()
        => Resources.Load<EnvironmentConfig>(OriginEnvPath);
}
