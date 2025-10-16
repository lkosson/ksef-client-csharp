
namespace KSeF.Client.Core.Models.Authorization
{

    public class AuthenticationOperationStatusResponse
    {
        public TokenInfo AccessToken { get; set; }
        public TokenInfo RefreshToken { get; set; }
    }

}
