using KSeF.Client.Api.Builders.SubUnitPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests
{
    public class SubUnitPermissionScenarioFixture
    {
        public string AccessToken { get; set; }
        public ContextIdentifier Unit { get; } = new ContextIdentifier
        {
            Type = ContextIdentifierType.Nip,
            Value = "0000000000"
        };

        public SubjectIdentifier SubUnit { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };

        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new();
        public int ExpectedPermissionsAfterRevoke { get; internal set; }
        public PagedPermissionsResponse<SubunitPermission> SearchResponse { get; internal set; }
    }

    [CollectionDefinition("SubUnitPermissionScenario")]
    public class SubUnitPermissionScenarioCollection
        : ICollectionFixture<SubUnitPermissionScenarioFixture>
    { }

    [Collection("SubUnitPermissionScenario")]
    public class SubUnitPermissionE2ETests : TestBase
    {
        private readonly SubUnitPermissionScenarioFixture _f;

        public SubUnitPermissionE2ETests(SubUnitPermissionScenarioFixture f)
        {
            _f = f;
            var authInfo = AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService).GetAwaiter().GetResult();
            _f.AccessToken = authInfo.AccessToken.Token;
            _f.Unit.Value = MiscellaneousUtils.GetRandomNip();
        }

        [Fact]
        public async Task SubUnitPermission_E2E_GrantAndRevoke()
        {
            // Nadaj uprawnienia sub-jednostce
            await Step1_GrantSubUnitPermissionsAsync();
            await Task.Delay(sleepTime);

            await Step2_SearchSubUnitAsync(expectAny: true);
            await Task.Delay(sleepTime);

            await Step3_RevokeSubUnitPermissionsAsync();
            await Task.Delay(sleepTime);

            //  Cofnij uprawnienia sub-jednostce
            await Step4_SearchSubUnitAsync(false);
        }

        public async Task Step1_GrantSubUnitPermissionsAsync()
        {
            var req = GrantSubUnitPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.SubUnit)
                .WithContext(_f.Unit)
                .WithDescription("E2E test grant sub-unit")
                .Build();

            var resp = await ksefClient
                .GrantsPermissionSubUnitAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            _f.GrantResponse = resp;
        }
        public async Task Step2_SearchSubUnitAsync(bool expectAny)
        {
            var rsp = await ksefClient
                .SearchEntityAuthorizationGrantsAsync(new Core.Models.Permissions.Entity.EntityAuthorizationsQueryRequest() {QueryType = Core.Models.Permissions.Entity.QueryType.Granted , PermissionTypes = new[] { Core.Models.Permissions.Entity.InvoicePermissionType.SelfInvoicing}.ToList() }, _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp = await ksefClient
                .SearchSubunitAdminPermissionsAsync(new Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp1 = await ksefClient
                .SearchGrantedPersonPermissionsAsync(new Core.Models.Permissions.Person.PersonPermissionsQueryRequest() { QueryType = Core.Models.Permissions.Person.QueryTypeEnum.PermissionsGrantedInCurrentContext, PermissionState = Core.Models.Permissions.Person.PermissionState.Inactive }, _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp2 = await ksefClient
                .SearchSubordinateEntityInvoiceRolesAsync(new Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp3 = await ksefClient
                .SearchEntityInvoiceRolesAsync(_f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp4 = await ksefClient
                .SearchGrantedEuEntityPermissionsAsync(new Core.Models.Permissions.EUEntity.EuEntityPermissionsQueryRequest() { PermissionTypes = new[] { Core.Models.Permissions.EUEntity.EuEntityPermissionsQueryPermissionType.InvoiceRead }.ToList() }, _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            Assert.NotNull(resp);
            if (expectAny)
            {
                Assert.Empty(resp.Permissions);
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

        public async Task Step3_RevokeSubUnitPermissionsAsync()
        {
            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await ksefClient
                .RevokeAuthorizationsPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

                Assert.NotNull(resp);
                Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
                _f.RevokeResponse.Add(resp);
            }

            foreach (var revokeStatus in _f.RevokeResponse)
            {
                await Task.Delay(sleepTime);
                var status = await ksefClient.OperationsStatusAsync(revokeStatus.OperationReferenceNumber, _f.AccessToken);
                if (status.Status.Code == 400 && status.Status.Description == "Operacja zakończona niepowodzeniem" && status.Status.Details.First() == "Permission cannot be revoked.")
                {
                    _f.ExpectedPermissionsAfterRevoke += 1;
                }
            }
        }

        public async Task Step4_SearchSubUnitAsync(bool expectAny)
    => await Step2_SearchSubUnitAsync(expectAny);
    }
}
