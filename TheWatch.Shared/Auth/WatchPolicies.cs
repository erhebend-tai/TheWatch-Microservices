namespace TheWatch.Shared.Auth;

/// <summary>
/// Static policy constants used across all services for authorization.
/// </summary>
public static class WatchPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ResponderAccess = "ResponderAccess";
    public const string DoctorAccess = "DoctorAccess";
    public const string FamilyAccess = "FamilyAccess";
    public const string ServiceToService = "ServiceToService";
    public const string Authenticated = "Authenticated";
}
