namespace MotelLease.Application.Common;

/// <summary>
/// Input patterns shared by validators. Deliberately conservative: they reject obvious junk
/// and leave semantic checks (address exists, code matches) to the handlers.
/// </summary>
public static class CommonRules
{
    /// <summary>Letters, digits, dot, underscore and dash; 3–64 characters.</summary>
    public const string UsernamePattern = @"^[a-zA-Z0-9._-]{3,64}$";

    /// <summary>
    /// One @ with a dotted domain. Not RFC 5322 — full compliance is unreadable and the OTP
    /// step is what actually proves an address works.
    /// </summary>
    public const string EmailPattern = @"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$";

    /// <summary>At least 8 characters with a letter and a digit.</summary>
    public const string PasswordPattern = @"^(?=.*[A-Za-z])(?=.*\d).{8,128}$";

    /// <summary>Vietnamese mobile or landline, optionally in +84 form.</summary>
    public const string PhonePattern = @"^(0|\+84)\d{8,10}$";

    public const string OtpPattern = @"^\d{6}$";

    /// <summary>
    /// Letters (including Vietnamese diacritics), spaces, apostrophes and dots. Digits are
    /// rejected so a full name cannot be used to smuggle markup or a phone number.
    /// </summary>
    public const string FullNamePattern = @"^[\p{L}][\p{L}\s'.\-]{1,127}$";
}
