using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;

namespace KSeF.Client.Tests
{
    public class IndirectPermissionScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier Subject { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };
        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
        
        public string Target { get; internal set; }
        public PagedPermissionsResponse<SubunitPermission> SearchResponse { get; internal set; }
        public int ExpectedPermissionsAfterRevoke { get; internal set; }
    }

    [CollectionDefinition("IndirectPermissionScenario")]
    public class IndirectPermissionScenarioCollection
        : ICollectionFixture<IndirectPermissionScenarioFixture>
    { }

    [Collection("IndirectPermissionScenario")]
    public class IndirectPermissionE2ETests : TestBase
    {
        private readonly IndirectPermissionScenarioFixture _f;

        public IndirectPermissionE2ETests(IndirectPermissionScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.Subject.Value = randomGenerator
                .Next(900000000, 999999999)
                .ToString();
            _f.Target = randomGenerator
                .Next(900000000, 999999999)
                .ToString();
        }

        [Fact]
        public async Task IndirectPermission_E2E_GrantSearchRevokeSearch()
        {
            await Step1_GrantIndirectAsync();
            await Task.Delay(sleepTime);

            //no info

            //await Step2_SearchIndirectAsync(expectAny: true);
            //await Task.Delay(sleepTime);

            //await Step3_RevokeIndirectAsync();
            //await Task.Delay(sleepTime);

            await Step4_SearchIndirectAsync(expectAny: false);
        }

        public async Task Step1_GrantIndirectAsync()
        {
            var req = GrantIndirectEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Subject)
                .WithContext(new TargetIdentifier() { Type = TargetIdentifierType.Nip, Value = _f.Target })                
                .WithPermissions(StandardPermissionType.InvoiceRead,
                    StandardPermissionType.InvoiceWrite)
                .WithDescription("E2E indirect grant")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionIndirectEntityAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;

            var status = await kSeFClient.OperationsStatusAsync(resp.OperationReferenceNumber,AccessToken);

            //Assert.True(status.)
        }

        public async Task Step2_SearchIndirectAsync(bool expectAny)
        {
            var rsp = await kSeFClient
                .SearchEntityAuthorizationGrantsAsync(new Core.Models.Permissions.Entity.EntityAuthorizationsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp = await kSeFClient
                .SearchSubunitAdminPermissionsAsync(new Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp1 = await kSeFClient
                .SearchGrantedPersonPermissionsAsync(new Core.Models.Permissions.Person.PersonPermissionsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp2 = await kSeFClient
                .SearchSubordinateEntityInvoiceRolesAsync(new Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp3 = await kSeFClient
                .SearchEntityInvoiceRolesAsync(_f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp4 = await kSeFClient
                .SearchGrantedEuEntityPermissionsAsync(new(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);            

            Assert.NotNull(resp);
            if (expectAny)
            {
                Assert.NotEmpty(resp.Permissions);
                _f.SearchResponse = resp;
            }
            else
            {
                if (_f.ExpectedPermissionsAfterRevoke > 0)
                {
                    Assert.True(resp.Permissions.Count == _f.ExpectedPermissionsAfterRevoke);
                }
                else
                {
                    Assert.Empty(resp.Permissions);
                }
            }        
        }

        public async Task Step3_RevokeIndirectAsync()
        {

            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await kSeFClient.RevokeAuthorizationsPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

                Assert.NotNull(resp);
                Assert.True(string.IsNullOrEmpty(resp.OperationReferenceNumber));
                _f.RevokeResponse.Add(resp);
            }

            foreach (var revokeStatus in _f.RevokeResponse)
            {
                await Task.Delay(sleepTime);
                var status = await kSeFClient.OperationsStatusAsync(revokeStatus.OperationReferenceNumber, AccessToken);
                if (status.Status.Code == 400 && status.Status.Description == "Operacja zakończona niepowodzeniem" && status.Status.Details.First() == "Permission cannot be revoked.")
                {
                    _f.ExpectedPermissionsAfterRevoke += 1;
                }
            }
        }

        public async Task Step4_SearchIndirectAsync(bool expectAny)
            => await Step2_SearchIndirectAsync(expectAny);
    }
}
