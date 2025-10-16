using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Tests;
using KSeF.Client.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace KSeF.Client.Tests.Core.E2E.TestData
{
    public class TestDataE2ETests : TestBase
    {
        protected ITestDataClient _testDataClient => _scope.ServiceProvider.GetRequiredService<ITestDataClient>();

        /// <summary>
        /// Test weryfikuje poprawność tworzenia i usuwania testowego podmiotu.
        /// Sprawdza czy wraz z usunięciem testowego podmiotu zostają automatycznie usunięte jego jednostki podrzędne.
        /// </summary>
        [Fact]
        public async Task CreateSubject_ThenRemoveSubject()
        {
            // Arrange - Przygotowanie danych testowych podmiotu
            string subjectNip = MiscellaneousUtils.GetRandomNip();
            string subunitNip = MiscellaneousUtils.GetRandomNip();

            SubjectCreateRequest createRequest = new SubjectCreateRequest
            {
                SubjectNip = subjectNip,
                SubjectType = SubjectType.VatGroup,
                Subunits = new List<SubjectSubunit>
            {
                new SubjectSubunit
                {
                    SubjectNip = subunitNip,
                    Description = "Jednostka podrzędna grupy VAT"
                }
            },
                Description = "Grupa VAT"
            };

            // Act - Utworzenie testowego podmiotu w systemie
            await _testDataClient.CreateSubjectAsync(createRequest);

            // Usunięcie testowego podmiotu - w przypadku grupy VAT i JST zostaną automatycznie usunięte wszystkie jednostki podrzędne
            SubjectRemoveRequest removeRequest = new SubjectRemoveRequest
            {
                SubjectNip = subjectNip
            };
            await _testDataClient.RemoveSubjectAsync(removeRequest);
        }

        /// <summary>
        /// Test weryfikuje poprawność tworzenia i usuwania osoby fizycznej w systemie.
        /// Sprawdza operacje na osobach fizycznych z flagą komornika.
        /// </summary>
        [Fact]
        public async Task CreatePerson_ThenRemovePerson()
        {
            // Arrange - Przygotowanie danych osoby fizycznej
            string personNip = MiscellaneousUtils.GetRandomNip();
            string personPesel = MiscellaneousUtils.GetRandomPesel();

            PersonCreateRequest createRequest = new PersonCreateRequest
            {
                Nip = personNip,
                Pesel = personPesel,
                IsBailiff = true,
                Description = "Osoba fizyczna - test tworzenia i usuwania"
            };

            // Act - Utworzenie testowej osoby fizycznej w systemie
            await _testDataClient.CreatePersonAsync(createRequest);

            // Usunięcie testowej osoby fizycznej z systemu
            PersonRemoveRequest removeRequest = new PersonRemoveRequest
            {
                Nip = personNip
            };
            await _testDataClient.RemovePersonAsync(removeRequest);
        }

        /// <summary>
        /// Test weryfikuje poprawność nadawania i odbierania uprawnień do wysyłki faktur z załącznikami.
        /// Sprawdza cykl życia uprawnień dla podmiotu.
        /// </summary>s
        [Fact]
        public async Task GrantAttachmentPermission_ThenRevokePermission()
        {
            // Arrange - Przygotowanie danych uprawnień do załączników
            string nip = MiscellaneousUtils.GetRandomNip();

            // Act - Nadanie uprawnienia do wysyłki faktur z załącznikami
            AttachmentPermissionGrantRequest grantPermissionRequest = new AttachmentPermissionGrantRequest
            {
                Nip = nip
            };
            await _testDataClient.EnableAttachmentAsync(grantPermissionRequest);

            // Odebranie uprawnienia do wysyłki faktur z załącznikami
            AttachmentPermissionRevokeRequest revokePermissionRequest = new AttachmentPermissionRevokeRequest
            {
                Nip = nip
            };
            await _testDataClient.DisableAttachmentAsync(revokePermissionRequest);
        }
    }
}
