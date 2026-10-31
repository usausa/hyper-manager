namespace HyperManager.Agent.HyperV;

using System.Security.Principal;

public static class ElevationChecker
{
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
