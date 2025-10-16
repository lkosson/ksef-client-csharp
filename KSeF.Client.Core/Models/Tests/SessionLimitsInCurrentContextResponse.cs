namespace KSeF.Client.Core.Models.Tests
{
    public class SessionLimitsInCurrentContextResponse
    {
        public SessionLimitsBase OnlineSession { get; set; }
        public SessionLimitsBase BatchSession { get; set; }
    }
}
