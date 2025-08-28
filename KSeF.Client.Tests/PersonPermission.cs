using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests
{
    public class PersonPermissionScenarioFixture
    {
        public string AccessToken { get; set; }

        // Testowy użytkownik — tu wstaw realny identyfikator (NIP lub PESEL)
        public SubjectIdentifier Person { get; } =
            new SubjectIdentifier
            {
                Value = "0000000000",
                Type = SubjectIdentifierType.Pesel
            };

        public OperationResponse GrantResponse { get; set; }
        public List<OperationResponse> RevokeResponse { get; set; } = new();
        public PagedPermissionsResponse<PersonPermission> SearchResponse { get; set; }
        public int ExpectedPermissionsAfterRevoke { get; internal set; }
    }

    [CollectionDefinition("PersonPermissionScenario")]
    public class PersonPermissionScenarioCollection
        : ICollectionFixture<PersonPermissionScenarioFixture>
    { }

    [Collection("PersonPermissionScenario")]
    public class PersonPermissionE2ETests : TestBase
    {
        private readonly PersonPermissionScenarioFixture _f;

        public PersonPermissionE2ETests(PersonPermissionScenarioFixture f)
        {
            _f = f;
            var authInfo = AuthenticationUtils.AuthenticateAsync(ksefClient, signatureService).GetAwaiter().GetResult();
            _f.Person.Value = MiscellaneousUtils.GetRandomNip();
            _f.AccessToken = authInfo.AccessToken.Token;
            _f.Person.Value = MiscellaneousUtils.GetRandomPesel();
        }

        [Fact]
        public async Task PersonPermission_E2E_GrantSearchRevokeSearch()
        {
            // 1. Nadaj uprawnienia
            await Step1_GrantPermissionsAsync();
            await Task.Delay(sleepTime);
            // 2. Wyszukaj — powinny się pojawić
            await Step2_SearchGrantedPermissionsAsync(expectAny: true);
            await Task.Delay(sleepTime);
            // 3. Cofnij uprawnienia
            await Step3_RevokePermissionsAsync();
            await Task.Delay(sleepTime);
            // 4. Wyszukaj ponownie — nie powinno być wpisów
            await Step4_SearchGrantedPermissionsAsync(expectAny: false);
        }

        public async Task Step1_GrantPermissionsAsync()
        {
            var req = GrantPersonPermissionsRequestBuilder
                .Create()
                .WithSubject(_f.Person)
                .WithPermissions(
                    StandardPermissionType.InvoiceRead,
                    StandardPermissionType.InvoiceWrite)
                .WithDescription("E2E test grant")
                .Build();

            var resp = await ksefClient
                .GrantsPermissionPersonAsync(req, _f.AccessToken, CancellationToken.None);

            Assert.NotNull(resp);
            Assert.True(!string.IsNullOrEmpty(resp.OperationReferenceNumber));
            _f.GrantResponse = resp;
        }

        public async Task Step2_SearchGrantedPermissionsAsync(bool expectAny)
        {
            var query = new PersonPermissionsQueryRequest
            {
                PermissionTypes = new List<PersonPermissionType>
                {
                    PersonPermissionType.InvoiceRead,
                    PersonPermissionType.InvoiceWrite
                }
            };

            var resp = await ksefClient
                .SearchGrantedPersonPermissionsAsync(
                    query,
                    _f.AccessToken,
                    pageOffset: 0,
                    pageSize: 10,
                    CancellationToken.None);

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

        public async Task Step3_RevokePermissionsAsync()
        {
            foreach (var permission in _f.SearchResponse.Permissions)
            {
                var resp = await ksefClient
                .RevokeCommonPermissionAsync(permission.Id, _f.AccessToken, CancellationToken.None);

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

        public async Task Step4_SearchGrantedPermissionsAsync(bool expectAny)
        {
            await Step2_SearchGrantedPermissionsAsync(expectAny);
        }
    }
}
