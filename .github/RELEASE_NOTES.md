# Changelog zmian – `## 2.0.0 (2025-07-14)` (KSeF.Client)

> Info: 🔧 zmienione • ➕ dodane • ➖ usunięte

---

## 1. KSeF.Client

### 1.1 Api/Services
- **AuthCoordinator.cs**: 🔧 Dodano dodatkowy log `Status.Details`; 🔧 dodano wyjątek przy `Status.Code == 400`; ➖ usunięto `ipAddressPolicy`
- **CryptographyService.cs**: ➕ inicjalizacja certyfikatów; ➕ pola `symetricKeyEncryptionPem`, `ksefTokenPem`
- **SignatureService.cs**: 🔧 `Sign(...)` → `SignAsync(...)`
- **QrCodeService.cs**: ➕ nowa usługa do generowania QrCodes
- **VerificationLinkService.cs**: ➕ nowa usługa generowania linków do weryfikacji faktury

### 1.2 Api/Builders
- **SendCertificateEnrollmentRequestBuilder.cs**: 🔧 `ValidFrom` pole zmienione na opcjonalne ; ➖ interfejs `WithValidFrom`
- **OpenBatchSessionRequestBuilder.cs**: 🔧 `WithBatchFile(...)` usunięto parametr `offlineMode`; ➕ `WithOfflineMode(bool)` nopwy opcjonalny krok do oznaczenia trybu offline

### 1.3 Core/Models
- **StatusInfo.cs**: 🔧 dodano property `Details`; ➖ `BasicStatusInfo` - usunięto klase w c elu unifikacji statusów
- **PemCertificateInfo.cs**: ➕ `PublicKeyPem` - dodano nowe property
- **DateType.cs**: ➕ `Invoicing`, `Acquisition`, `Hidden` - dodano nowe emumeratory do filtrowania faktur
- **PersonPermission.cs**: 🔧 `PermissionScope` zmieniono z PermissionType zgodnie ze zmianą w kontrakcie
- **PersonPermissionsQueryRequest.cs**: 🔧 `QueryType` - dodano nowe wymagane property do filtrowania w zadanym kontekście
- **SessionInvoice.cs**: 🔧 `InvoiceFileName` - dodano nowe property 
- **ActiveSessionsResponse.cs** / `Status.cs` / `Item.cs` (Sessions): ➕ nowe modele

### 1.4 Core/Interfaces
- **IKSeFClient.cs**: 🔧 `GetAuthStatusAsync` → zmiana modelu zwracanego z `BasicStatusInfo` na `StatusInfo` 
➕ Dodano metodę GetActiveSessions(accessToken, pageSize, continuationToken, cancellationToken)
➕ Dodano metodę RevokeCurrentSessionAsync(token, cancellationToken)
➕ Dodano metodę RevokeSessionAsync(referenceNumber, accessToken, cancellationToken)
- **ISignatureService.cs**: 🔧 `Sign` → `SignAsync`
- **IQrCodeService.cs**: nowy interfejs do generowania QRcodes 
- **IVerificationLinkService.cs**: ➕ nowy interfejs do tworzenia linków weryfikacyjnych do faktury

### 1.5 DI & Dependencies
- **ServiceCollectionExtensions.cs**: ➕ rejestracja `IQrCodeService`, `IVerificationLinkService`
- **KSeFClientOptions.cs**: 🔧 walidacja `BaseUrl`
➕ Dodano CustomHeaders - umożliwia dodawanie dodatkowych nagłówków do klienta Http
- **KSeF.Client.csproj**: ➕ `QRCoder`, `System.Drawing.Common`

### 1.6 Http
- **KSeFClient.cs**: ➕ nagłówki `X-KSeF-Session-Id`, `X-Environment`; ➕ `Content-Type: application/octet-stream`

---

## 2. KSeF.Client.Tests
**Nowe pliki**: `QrCodeTests.cs`, `VerificationLinkServiceTests.cs`  
Wspólne: 🔧 `Thread.Sleep` → `Task.Delay`; ➕ `ExpectedPermissionsAfterRevoke`; 4-krokowy flow; obsługa 400  
Wybrane: **Authorization.cs**, `EntityPermission*.cs`, **OnlineSession.cs**, **TestBase.cs**

---

## 3. KSeF.DemoWebApp/Controllers
- **QrCodeController.cs**: ➕ `GET /qr/certificate` ➕`/qr/invoice/ksef` ➕`qr/invoice/offline`
- **ActiveSessionsController.cs**: ➕ `GET /sessions/active`
- **AuthController.cs**: ➕ `GET /auth-with-ksef-certificate`; 🔧 fallback `contextIdentifier`
- **BatchSessionController.cs**: ➕ `WithOfflineMode(false)`; 🔧 pętla `var`
- **CertificateController.cs**: ➕ `serialNumber`, `name`; ➕ builder
- **OnlineSessionController.cs**: ➕ `WithOfflineMode(false)`

---

## 4. Podsumowanie

| Typ zmiany | Liczba plików |
|------------|---------------|
| ➕ dodane   | 12 |
| 🔧 zmienione| 32 |
| ➖ usunięte | 2 |

---
