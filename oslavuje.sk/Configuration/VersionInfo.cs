using System.Reflection;

public static class VersionInfo
{
    public static string GetVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var informationalVersion = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return informationalVersion ?? "Neznáma verzia";
    }
}