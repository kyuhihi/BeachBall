using UnityEngine;

public static class UltimateConfigLoader
{
    // Resources ���� ���(Ȯ����/Assets/ ����)
    public const string FoxUltimatePath = "UltimateSetting/FoxUltimateSetting";
    public const string TurtleUltimatePath = "UltimateSetting/TurtleUltimateSetting";
    public const string MonkeyUltimatePath = "UltimateSetting/MonkeyUltimateSetting";
    public const string OriginEnvPath   = "OriginSkySetting";

    public static UltimateSetting LoadFoxUltimate()
        => Resources.Load<UltimateSetting>(FoxUltimatePath);

    public static UltimateSetting LoadTurtleUltimate()
        => Resources.Load<UltimateSetting>(TurtleUltimatePath);
    public static UltimateSetting LoadMonkeyUltimate()
        => Resources.Load<UltimateSetting>(MonkeyUltimatePath);

    public static EnvironmentConfig LoadOriginEnv()
        => Resources.Load<EnvironmentConfig>(OriginEnvPath);
}
