using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class TemiContactVM
{
    [MinLength(1)]
    [Display(Name = nameof(Name), ResourceType = typeof(Resources.Temi))]
    [Required(ErrorMessageResourceName = "ErrorMessageRequired", ErrorMessageResourceType = typeof(Resources.Temi))]
    public string Name { get; set; } = default!;

    [MinLength(1)]
    [Display(Name = nameof(Topic), ResourceType = typeof(Resources.Temi))]
    [Required(ErrorMessageResourceName = "ErrorMessageRequired", ErrorMessageResourceType = typeof(Resources.Temi))]
    public string Topic { get; set; } = default!;

    [DataType(DataType.EmailAddress, ErrorMessageResourceName = "ErrorMessageEmail", ErrorMessageResourceType = typeof(Resources.Temi))]
    [Display(Name = nameof(EMail), ResourceType = typeof(Resources.Temi))]
    public string EMail { get; set; } = default!;

    [DataType(DataType.PhoneNumber)]
    [Display(Name = nameof(Tel), ResourceType = typeof(Resources.Temi))]
    public string? Tel { get; set; }
}