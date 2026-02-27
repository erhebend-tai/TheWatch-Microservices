namespace TheWatch.P5.AuthSecurity.Models;

public class EulaVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsCurrent { get; set; }
}

public class EulaAcceptance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid EulaVersionId { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
