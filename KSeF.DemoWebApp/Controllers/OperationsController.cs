using KSeF.Client;
using KSeF.Client.Core.Models.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OperationsController : ControllerBase
    {
        private readonly IKSeFClient _client;

        public OperationsController(IKSeFClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Sprawdzenie statusu zgody na wystawianie faktur z załącznikiem.
        /// </summary>
        [HttpGet("attachments/status")]
        [ProducesResponseType(typeof(OperationResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<OperationResponse>> GetAttachmentPermissionStatus(
            [FromHeader(Name = "Authorization")] string accessToken,
            CancellationToken cancellationToken)
        {
            var result = await _client.GetAttachmentPermissionStatusAsync(accessToken, cancellationToken);
            return Ok(result);
        }
    }
}
