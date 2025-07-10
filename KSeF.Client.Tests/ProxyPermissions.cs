using KSeF.Client.Api.Builders.ProxyEntityPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.ProxyEntity;

namespace KSeF.Client.Tests
{
    public class ProxyPermissionsScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier Entity { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Nip,
            Value = "0000000000"
        };

        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new();
        public PagedRolesResponse<EntityRole> SearchResponse { get; set; }
        public int ExpectedPermissionsAfterRevoke { get; internal set; }
    }

    [CollectionDefinition("ProxyPermissionsScenario")]
    public class ProxyPermissionsScenarioCollection
        : ICollectionFixture<ProxyPermissionsScenarioFixture>
    { }

    [Collection("ProxyPermissionsScenario")]
    public class ProxyPermissionsE2ETests : TestBase
    {
        private readonly ProxyPermissionsScenarioFixture _f;

        public ProxyPermissionsE2ETests(ProxyPermissionsScenarioFixture f)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.Entity.Value = NIP;
            _f.Entity.Value = randomGenerator
                .Next(900000000, 999999999)
                .ToString() + "00";
        }

        [Fact]
        public async Task ProxyPermissions_E2E_GrantSearchRevokeSearch()
        {
            // 1. Nadaj uprawnienia
            await Step1_GrantPermissionsAsync();
            await Task.Delay(sleepTime);

            // 2. Wyszukaj — powinny się pojawić
            //TODO ustalić czy coś powinno się pojawić, ewentualnie użyć endpointa dedykowanego jeśli zostanie zaimplementowany
            await Step2_SearchGrantedRolesAsync(expectAny: true);
            await Task.Delay(sleepTime);

            // 3. Cofnij uprawnienia
            await Step3_RevokePermissionsAsync();
            await Task.Delay(sleepTime);

            // 4. Wyszukaj ponownie — nie powinno być wpisów
            await Step4_SearchGrantedPermissionsAsync(expectAny: false);
        }

        public async Task Step1_GrantPermissionsAsync()
        {
            var req = GrantProxyEntityPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Entity)
                .WithPermission(StandardPermissionType.SelfInvoicing)
                .WithDescription("E2E test grant")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionProxyEntityAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.True(!string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchGrantedRolesAsync(bool expectAny)
        {
            var resp = await kSeFClient
                .SearchEntityInvoiceRolesAsync(
                    _f.AccessToken,
                    pageOffset: 0,
                    pageSize: 10,
                    CancellationToken.None
                );

            //500 ???
            var rsp = await kSeFClient
            .SearchEntityAuthorizationGrantsAsync(new Core.Models.Permissions.Entity.EntityAuthorizationsQueryRequest() { QueryType = Core.Models.Permissions.Entity.QueryType.Received,  }, _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp3 = await kSeFClient
                .SearchSubunitAdminPermissionsAsync(new Core.Models.Permissions.SubUnit.SubunitPermissionsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp1 = await kSeFClient
                .SearchGrantedPersonPermissionsAsync(new Core.Models.Permissions.Person.PersonPermissionsQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp2 = await kSeFClient
                .SearchSubordinateEntityInvoiceRolesAsync(new Core.Models.Permissions.SubUnit.SubordinateEntityRolesQueryRequest(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

            var resp4 = await kSeFClient
                .SearchGrantedEuEntityPermissionsAsync(new(), _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);
            
            Assert.NotNull(resp);
            if (expectAny)
            {
                Assert.Empty(resp.Roles);
                _f.SearchResponse = resp;
            }
            else
            {
                if (_f.ExpectedPermissionsAfterRevoke > 0)
                {
                    Assert.True(resp.Roles.Count == _f.ExpectedPermissionsAfterRevoke);
                }
                else
                {
                    Assert.Empty(resp.Roles);
                }
            }
        }

        public async Task Step3_RevokePermissionsAsync()
        {            

            foreach (var permission in _f.SearchResponse.Roles)
            {
                var resp = await kSeFClient
                .RevokeAuthorizationsPermissionAsync(permission.Role, _f.AccessToken, CancellationToken.None);

                Assert.NotNull(resp);
                Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
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

        public async Task Step4_SearchGrantedPermissionsAsync(bool expectAny)
            => await Step2_SearchGrantedRolesAsync(expectAny);
    }
}
