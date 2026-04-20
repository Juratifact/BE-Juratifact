using System.ComponentModel.DataAnnotations;

namespace Juratifact.Service.CloudinaryService;

public class CloudinaryOptions
{
    [Required]public string CloudName { get; set; }
    [Required]public string ApiKey { get; set; }
    [Required]public string ApiSecret { get; set; }
}