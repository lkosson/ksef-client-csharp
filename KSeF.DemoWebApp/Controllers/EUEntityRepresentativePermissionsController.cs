using KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;
using Microsoft.AspNetCore.Mvc;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;

namespace KSeF.DemoWebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class EUEntityRepresentativePermissionsController(IKSeFClient ksefClient) : ControllerBase
{
    [HttpPost("grant-eu-entity-representative-permissions")]
    public async Task<ActionResult<OperationResponse>> GrantPermissionsEntity(string accessToken, EUEntitRepresentativeSubjectIdentifier subjectIdentifier, CancellationToken cancellationToken)
    {
        GrantPermissionsEUEntitRepresentativeRequest request = GrantEUEntityRepresentativePermissionsRequestBuilder
            .Create()
            .WithSubject(subjectIdentifier)
            .WithPermissions(EUEntitRepresentativeStandardPermissionType.InvoiceRead, EUEntitRepresentativeStandardPermissionType.InvoiceWrite)
            .WithDescription("Representative access")
            .Build();

        return await ksefClient.GrantsPermissionEUEntityRepresentativeAsync(request, accessToken, cancellationToken);
    }

    [HttpPost("revoke-eu-entity-representative-permissions")]
    public async Task<ActionResult<OperationResponse>> RevokePermissionsEntity(string accessToken, string permissionId, CancellationToken cancellationToken)
    {
  
        return await ksefClient.RevokeAuthorizationsPermissionAsync(permissionId, accessToken, cancellationToken);
    }
}
