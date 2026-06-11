using System.ComponentModel.DataAnnotations;

namespace TmsApi;

public class PaymentOptions
{
    [Required(ErrorMessage = "GatewayUrl is required")]
    [Url(ErrorMessage = "GatewayUrl must be a valid URL")]
    public string GatewayUrl { get; set; } = string.Empty;

    [Required]
    [Range(100, 100000, ErrorMessage= "MaxDepositBirr must be between 100 and 100,000 ")]
    public decimal MaxDepositBirr { get; set; }
}