namespace MotelLease.Application.Common.Errors;

/// <summary>
/// Every user-facing string the handlers can produce, as a key. The text lives in
/// Infrastructure/Localization/Resources/messages.*.json.
/// </summary>
public static class MessageKeys
{
    /// <summary>Used by the Api layer when it answers before a handler runs.</summary>
    public static class General
    {
        public const string ValidationFailed = "error.validation.title";
        public const string Unexpected = "error.unexpected";
        public const string Unauthorized = "error.unauthorized";
        public const string Forbidden = "error.forbidden";
        public const string RateLimited = "error.rate_limited";
    }

    public static class Auth
    {
        public const string InvalidCredentials = "error.auth.invalid_credentials";
        public const string AccountLocked = "error.auth.account_locked";
        public const string EmailTaken = "error.auth.email_taken";
        public const string UsernameTaken = "error.auth.username_taken";
        public const string EmailNotVerified = "error.auth.email_not_verified";
        public const string RoleNotSelfAssignable = "error.auth.role_not_self_assignable";
        public const string RefreshTokenInvalid = "error.auth.refresh_token_invalid";
        public const string RefreshTokenReused = "error.auth.refresh_token_reused";
        public const string GoogleTokenInvalid = "error.auth.google_token_invalid";
        public const string GoogleEmailUnverified = "error.auth.google_email_unverified";
        public const string PasswordNotSet = "error.auth.password_not_set";
        public const string CurrentPasswordWrong = "error.auth.current_password_wrong";
        public const string NewPasswordSameAsCurrent = "error.auth.new_password_same";
    }

    public static class Otp
    {
        public const string ResendTooSoon = "error.otp.resend_too_soon";
        public const string NotFound = "error.otp.not_found";
        public const string Mismatch = "error.otp.mismatch";
        public const string TooManyAttempts = "error.otp.too_many_attempts";
    }

    public static class Account
    {
        public const string NotFound = "error.account.not_found";
        public const string SessionNotFound = "error.account.session_not_found";
        public const string EmailUnchanged = "error.account.email_unchanged";
        public const string AvatarTooLarge = "error.account.avatar_too_large";
        public const string AvatarTypeNotSupported = "error.account.avatar_type_not_supported";
        public const string LanguageNotSupported = "error.account.language_not_supported";
    }

    public static class Image
    {
        public const string Rejected = "error.image.rejected";
        public const string TooLarge = "error.image.too_large";
        public const string TypeNotSupported = "error.image.type_not_supported";
        public const string NotFound = "error.image.not_found";
    }

    public static class BoardingHouse
    {
        public const string NotFound = "error.boarding_house.not_found";
        public const string NotYours = "error.boarding_house.not_yours";
        public const string InUse = "error.boarding_house.in_use";
        public const string AlreadyUnderReview = "error.boarding_house.already_under_review";
        public const string NothingToPublish = "error.boarding_house.nothing_to_publish";
        public const string TypeConflictsWithOccupancy =
            "error.boarding_house.type_conflicts_with_occupancy";
        public const string TooManyImages = "error.boarding_house.too_many_images";
    }

    public static class RoomType
    {
        public const string NotFound = "error.room_type.not_found";
        public const string FacilityNotFound = "error.room_type.facility_not_found";
        public const string SingleOccupantHouse = "error.room_type.single_occupant_house";
        public const string MaxOccupantsBelowLive = "error.room_type.max_occupants_below_live";
        public const string InUse = "error.room_type.in_use";
    }

    public static class Room
    {
        public const string NotFound = "error.room.not_found";
        public const string NumberTaken = "error.room.number_taken";
        public const string RoomTypeFromAnotherHouse = "error.room.room_type_from_another_house";
        public const string StatusNotManuallySettable = "error.room.status_not_manually_settable";
        public const string StatusLockedByOccupancy = "error.room.status_locked_by_occupancy";
        public const string ReadingWentBackwards = "error.room.reading_went_backwards";
        public const string InUse = "error.room.in_use";
    }

    public static class Email
    {
        public const string RegistrationOtpSubject = "email.registration_otp.subject";
        public const string RegistrationOtpBody = "email.registration_otp.body";
        public const string PasswordResetOtpSubject = "email.password_reset_otp.subject";
        public const string PasswordResetOtpBody = "email.password_reset_otp.body";
        public const string EmailChangeOtpSubject = "email.email_change_otp.subject";
        public const string EmailChangeOtpBody = "email.email_change_otp.body";
    }

    public static class Validation
    {
        public const string Required = "validation.required";
        public const string EmailFormat = "validation.email_format";
        public const string UsernameFormat = "validation.username_format";
        public const string PasswordTooWeak = "validation.password_too_weak";
        public const string PhoneFormat = "validation.phone_format";
        public const string OtpFormat = "validation.otp_format";
        public const string TooLong = "validation.too_long";
        public const string FullNameFormat = "validation.full_name_format";
        public const string NotNegative = "validation.not_negative";
        public const string Positive = "validation.positive";
        public const string LatitudeRange = "validation.latitude_range";
        public const string LongitudeRange = "validation.longitude_range";
        public const string OutOfRange = "validation.out_of_range";
    }
}
