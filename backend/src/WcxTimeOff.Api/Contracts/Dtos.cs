using System.ComponentModel.DataAnnotations;
using WcxTimeOff.Api.Domain;

namespace WcxTimeOff.Api.Contracts;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public sealed class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class AgentOverviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public CreditBalanceDto Credits { get; set; } = new();
}

public sealed class CreditBalanceDto
{
    public decimal EarnedCredits { get; set; }
    public decimal TakenCredits { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal PendingReservedHours { get; set; }
    public decimal RequestableBalance { get; set; }
}

public sealed class UpdateEarnedCreditsRequest
{
    [Required]
    [Range(0, 100000, ErrorMessage = "Earned credits must be zero or greater.")]
    public decimal? EarnedCredits { get; set; }
}

public sealed class CreateTimeOffRequest
{
    [Required]
    public DateTime? StartDate { get; set; }

    [Required]
    public DateTime? EndDate { get; set; }

    [Required]
    public DurationType? DurationType { get; set; }

    [Range(0.25, 8, ErrorMessage = "Partial-day hours must be greater than zero and at most 8.")]
    public decimal? RequestedHours { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public sealed class TimeOffRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DurationType { get; set; } = string.Empty;
    public decimal RequestedHours { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public sealed class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
