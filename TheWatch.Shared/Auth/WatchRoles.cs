namespace TheWatch.Shared.Auth;

/// <summary>
/// Static role constants used across all services for RBAC.
/// </summary>
public static class WatchRoles
{
    public const string Admin = "Admin";
    public const string Responder = "Responder";
    public const string FamilyMember = "FamilyMember";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";
    public const string ServiceAccount = "ServiceAccount";

    public static readonly string[] All =
    [
        Admin, Responder, FamilyMember, Doctor, Patient, ServiceAccount
    ];
}
