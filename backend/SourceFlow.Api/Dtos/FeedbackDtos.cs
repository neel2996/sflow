using System.ComponentModel.DataAnnotations;

namespace SourceFlow.Api.Dtos;

public class SubmitFeedbackRequest
{
    public string? Email { get; set; }

    [Required, MinLength(1)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^(feedback|bug|feature)$", ErrorMessage = "Type must be feedback, bug, or feature")]
    public string Type { get; set; } = "feedback";
}
