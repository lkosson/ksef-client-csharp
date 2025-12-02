using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Validation;
using System.Text.RegularExpressions;

namespace KSeF.Client.Api.Builders.AuthorizationEntityPermissions
{
    /// <summary>
    /// Buduje zapytanie o OTRZYMANE uprawnienia podmiotowe jako WŁAŚCICIEL w kontekście NIP.
    /// </summary>
    public static class EntityAuthorizationsQueryRequestBuilder
    {
        public static IOwnerNipStep Create() => new Impl();

        public interface IOwnerNipStep
        {
            IOptionalStep ReceivedForOwnerNip(string ownerNip);
        }

        public interface IOptionalStep
        {
            IOptionalStep WithPermissionTypes(IEnumerable<InvoicePermissionType> types);
            EntityAuthorizationsQueryRequest Build();
        }

        private sealed class Impl : IOwnerNipStep, IOptionalStep
        {
            private readonly EntityAuthorizationsQueryRequest _request = new();

            public IOptionalStep ReceivedForOwnerNip(string ownerNip)
            {
                if (string.IsNullOrWhiteSpace(ownerNip))
                {
                    throw new ArgumentNullException(nameof(ownerNip));
                }
                if (!RegexPatterns.Nip.IsMatch(ownerNip))
                {
                    throw new ArgumentException($"Nip: {ownerNip} jest nieprawidłowy.", nameof(ownerNip));
                }

                _request.AuthorizedIdentifier = new EntityAuthorizationsAuthorizedEntityIdentifier
                {
                    Type = EntityAuthorizationsAuthorizedEntityIdentifierType.Nip,
                    Value = ownerNip
                };
                _request.QueryType = QueryType.Received;

                return this;
            }

            public IOptionalStep WithPermissionTypes(IEnumerable<InvoicePermissionType> types)
            {
                _request.PermissionTypes = types is null ? null : [.. types];
                return this;
            }

            public EntityAuthorizationsQueryRequest Build() => _request;
        }
    }
}
