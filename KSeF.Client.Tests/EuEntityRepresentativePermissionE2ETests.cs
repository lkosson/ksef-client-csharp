using KSeF.Client.Api.Builders.EUEntityRepresentativePermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.EUEntityRepresentative;

namespace KSeF.Client.Tests
{
    public class EuEntityRepresentativeScenarioFixture
    {
        public string AccessToken { get; set; }
        public SubjectIdentifier EuEntity { get; } = new SubjectIdentifier
        {
            Type = SubjectIdentifierType.Fingerprint,
            Value = "00987654321"
        };
        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new List<OperationResponse>();
        public PagedPermissionsResponse<EuEntityPermission> SearchResponse { get; set; }
        public int ExpectedPermissionsAfterRevoke { get; internal set; }
    }

    [CollectionDefinition("EuEntityRepresentativeScenario")]
    public class EuEntityRepresentativeScenarioCollection
        : ICollectionFixture<EuEntityRepresentativeScenarioFixture>
    { }

    [Collection("EuEntityRepresentativeScenario")]
    public class EuEntityRepresentativePermissionE2ETests : TestBase
    {
        private readonly EuEntityRepresentativeScenarioFixture _f;

        public EuEntityRepresentativePermissionE2ETests(EuEntityRepresentativeScenarioFixture f) : base(Core.Models.Authorization.ContextIdentifierType.NipVatUe)
        {
            _f = f;
            _f.AccessToken = AccessToken;
            _f.EuEntity.Value = "73" + randomGenerator.Next(100000000, 999999999);
        }

        [Fact]
        public async Task EuEntityRepresentative_E2E_GrantSearchRevokeSearch()
        {
            await Step1_GrantEuRepAsync();
            await Task.Delay(sleepTime);

            await Step2_SearchEuRepAsync(expectAny: true);
            await Task.Delay(sleepTime);

            await Step3_RevokeEuRepAsync();
            await Task.Delay(sleepTime);

            await Step4_SearchEuRepAsync(expectAny: false);
        }

        public async Task Step1_GrantEuRepAsync()
        {
            var req = GrantEUEntityRepresentativePermissionsRequestBuilder
                .Create()
                .WithSubject(_f.EuEntity)
                .WithPermissions(StandardPermissionType.InvoiceWrite, StandardPermissionType.InvoiceRead)
                .WithDescription("E2E EU representative")
                .Build();

            var resp = await kSeFClient
                .GrantsPermissionEUEntityRepresentativeAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.False(string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchEuRepAsync(bool expectAny)
        {
            var query = new Core.Models.Permissions.EUEntity.EuEntityPermissionsQueryRequest { /* filtrowanie */ };
            var resp = await kSeFClient
                .SearchGrantedEuEntityPermissionsAsync(
                    query, _f.AccessToken, pageOffset: 0, pageSize: 10, CancellationToken.None);

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

        public async Task Step3_RevokeEuRepAsync()
        {
            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await kSeFClient
                .RevokeAuthorizationsPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

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

        public async Task Step4_SearchEuRepAsync(bool expectAny)
            => await Step2_SearchEuRepAsync(expectAny);
    }
}
