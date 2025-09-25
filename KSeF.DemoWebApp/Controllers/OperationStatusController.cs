using KSeF.Client.Core.Models.Permissions;
using KSeF.Client;
using Microsoft.AspNetCore.Mvc;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class OperationStatusController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpGet("{operationReferenceNumber}/status")]
    public async Task<ActionResult<PermissionsOperationStatusResponse>> GetOperationStatusAsync([FromRoute] string operationReferenceNumber, string accessToken, CancellationToken cancellationToken)
    {
        var status = await ksefClient.OperationsStatusAsync(operationReferenceNumber, accessToken, cancellationToken); 
        return Ok(status); 
    }
}
