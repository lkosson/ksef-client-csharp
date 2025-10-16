using KSeF.Client.Core.Infrastructure.Rest;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Tests;
using KSeF.Client.Core.Interfaces.Rest;

namespace KSeF.Client.Clients
{
    /// <inheritdoc />
    public sealed class TestDataClient : ClientBase, ITestDataClient
    {
        public TestDataClient(IRestClient rest, IRouteBuilder routeBuilder) : base(rest, routeBuilder)
        {
        }

        /// <inheritdoc />
        public Task CreateSubjectAsync(SubjectCreateRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.CreateSubject, request, cancellationToken);

        /// <inheritdoc />
        public Task RemoveSubjectAsync(SubjectRemoveRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.RemoveSubject, request, cancellationToken);

        /// <inheritdoc />
        public Task CreatePersonAsync(PersonCreateRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.CreatePerson, request, cancellationToken);

        /// <inheritdoc />
        public Task RemovePersonAsync(PersonRemoveRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.RemovePerson, request, cancellationToken);

        /// <inheritdoc />
        public Task GrantPermissionsAsync(TestDataPermissionsGrantRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.GrantPerms, request, cancellationToken);

        /// <inheritdoc />
        public Task RevokePermissionsAsync(TestDataPermissionsRevokeRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.RevokePerms, request, cancellationToken);

        /// <inheritdoc />
        public Task EnableAttachmentAsync(AttachmentPermissionGrantRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.EnableAttach, request, cancellationToken);

        /// <inheritdoc />
        public Task DisableAttachmentAsync(AttachmentPermissionRevokeRequest request, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.DisableAttach, request, cancellationToken);

        public Task ChangeSessionLimitsInCurrentContextAsync(ChangeSessionLimitsInCurrentContextRequest request, string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.ChangeSessionLimitsInCurrentContext, request, accessToken, cancellationToken);

        public Task RestoreDefaultSessionLimitsInCurrentContextAsync(string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.RestoreDefaultSessionLimitsInCurrentContext, HttpMethod.Delete, accessToken, cancellationToken);

        public Task ChangeCertificatesLimitInCurrentSubjectAsync(ChangeCertificatesLimitInCurrentSubjectRequest request, string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.ChangeCertificatesLimitInCurrentSubject, request, accessToken, cancellationToken);

        public Task RestoreDefaultCertificatesLimitInCurrentSubjectAsync(string accessToken, CancellationToken cancellationToken = default) =>
            ExecuteAsync(Routes.TestData.RestoreDefaultCertificatesLimitInCurrentSubject, HttpMethod.Delete, accessToken, cancellationToken);
    }
}
