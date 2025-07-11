
namespace KSeF.Client.Core.Models.Sessions.ActiveSessions;

public class ActiveSessionsResponse
{
    public string ContinuationToken { get; set; }
    public ICollection<Item> Items { get; set; }
}
