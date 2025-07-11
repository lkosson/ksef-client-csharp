
namespace KSeF.Client.Core.Models.Sessions.ActiveSessions;

public class Item
{
    public string ReferenceNumber { get; set; }
    public bool IsCurrent { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public string AuthenticationMethod { get; set; }
    public Status Status { get; set; }
    public bool IsTokenRedeemed { get; set; }
    public DateTimeOffset LastTokenRefreshDate { get; set; }
    public DateTimeOffset RefreshTokenValidUntil { get; set; }
}
