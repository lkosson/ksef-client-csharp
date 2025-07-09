using System.ComponentModel.DataAnnotations;

namespace KSeFClient.DI;

/// <summary>
/// Opcje konfiguracyjne dla klienta KSeF.
/// </summary>
public class KSeFClientOptions
{
    [Required(ErrorMessage = "BaseUrl is required.")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL.")]
    public string BaseUrl { get; set; } = "";
}
