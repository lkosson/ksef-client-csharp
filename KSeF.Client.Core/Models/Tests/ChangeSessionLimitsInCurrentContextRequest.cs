namespace KSeF.Client.Core.Models.Tests
{
    public sealed class ChangeSessionLimitsInCurrentContextRequest
    {
        public SessionLimitsBase OnlineSession { get; set; }
        public SessionLimitsBase BatchSession { get; set; }
    }
}
