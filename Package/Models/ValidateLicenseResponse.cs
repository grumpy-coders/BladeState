using System;

namespace GrumpyCoders.BladeState.Models;

public class ValidateLicenseResponse
{
    public bool IsValid { get; set; }
    public string ValidationReason { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
}