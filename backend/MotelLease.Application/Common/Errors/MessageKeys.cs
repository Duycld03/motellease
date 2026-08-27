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

    public static class Appointment
    {
        public const string NotFound = "error.appointment.not_found";
        public const string NotYours = "error.appointment.not_yours";
        public const string DateInPast = "error.appointment.date_in_past";
        public const string ListingNotPublished = "error.appointment.listing_not_published";
        public const string RoomNotAvailable = "error.appointment.room_not_available";
        public const string AlreadyRequested = "error.appointment.already_requested";
        public const string NotPending = "error.appointment.not_pending";
        public const string NotCancellable = "error.appointment.not_cancellable";
    }

    public static class Deposit
    {
        public const string NotFound = "error.deposit.not_found";
        public const string NotYours = "error.deposit.not_yours";
        public const string StartDateInPast = "error.deposit.start_date_in_past";
        public const string ListingNotPublished = "error.deposit.listing_not_published";
        public const string RoomNotAvailable = "error.deposit.room_not_available";
        public const string AlreadyRequested = "error.deposit.already_requested";
        public const string NotPending = "error.deposit.not_pending";
        public const string NotCancellable = "error.deposit.not_cancellable";
        public const string NotAccepted = "error.deposit.not_accepted";
        public const string NotPaid = "error.deposit.not_paid";
    }

    public static class Lease
    {
        public const string NotFound = "error.lease.not_found";
        public const string NotYours = "error.lease.not_yours";
        public const string RoomAlreadyLeased = "error.lease.room_already_leased";
        public const string RoomFullyOccupied = "error.lease.room_fully_occupied";
        public const string NotActive = "error.lease.not_active";
        public const string CannotRemovePrimaryTenant = "error.lease.cannot_remove_primary_tenant";
        public const string TenantAlreadyMovedOut = "error.lease.tenant_already_moved_out";
        public const string TenantNotFound = "error.lease.tenant_not_found";
        public const string NotTerminable = "error.lease.not_terminable";
        public const string ReadingBelowCurrent = "error.lease.reading_below_current";
    }

    public static class Extension
    {
        public const string NotFound = "error.extension.not_found";
        public const string NotYours = "error.extension.not_yours";
        public const string AlreadyPending = "error.extension.already_pending";
        public const string EndDateMustBeAfterCurrent = "error.extension.end_date_must_be_after_current";
        public const string NotPending = "error.extension.not_pending";
    }

    public static class AdditionalFee
    {
        public const string NotFound = "error.additional_fee.not_found";
        public const string AlreadyBilled = "error.additional_fee.already_billed";
    }

    public static class Payment
    {
        public const string NotFound = "error.payment.not_found";
        public const string ProviderNotAvailable = "error.payment.provider_not_available";
        public const string DepositNotAwaitingPayment = "error.payment.deposit_not_awaiting_payment";
        public const string DeadlinePassed = "error.payment.deadline_passed";
        public const string GatewayRejected = "error.payment.gateway_rejected";

        /// <summary>Shown on the gateway's own page, so it is resolved in the payer's language.</summary>
        public const string DepositDescription = "payment.deposit.description";
        public const string BillDescription = "payment.bill.description";

        /// <summary>
        /// Stored in RefundRequest.Reason when a payment lands after the room was released. The
        /// column normally holds what a tenant typed, so an automatic one carries a key instead of
        /// a sentence in whichever language the callback happened to arrive in.
        /// </summary>
        public const string RefundReasonPaidAfterDeadline =
            "refund.reason.paid_after_deadline";
    }

    public static class Bill
    {
        public const string NotFound = "error.bill.not_found";
        public const string NotYours = "error.bill.not_yours";
        public const string NotPayable = "error.bill.not_payable";
        public const string AlreadyExistsForPeriod = "error.bill.already_exists_for_period";
        public const string NoActiveLease = "error.bill.no_active_lease";
        public const string ReadingWentBackwards = "error.bill.reading_went_backwards";
        public const string NotDraft = "error.bill.not_draft";
        public const string NotCancellable = "error.bill.not_cancellable";
        public const string NoLiveTenants = "error.bill.no_live_tenants";
    }

    public static class Notification
    {
        public const string NotFound = "error.notification.not_found";
    }

    public static class SavedListing
    {
        public const string NotFound = "error.saved_listing.not_found";
        public const string AlreadySaved = "error.saved_listing.already_saved";
    }

    public static class Review
    {
        public const string NotFound = "error.review.not_found";
        public const string NotYours = "error.review.not_yours";
        public const string LeaseRequired = "error.review.lease_required";
        public const string AlreadyReviewed = "error.review.already_reviewed";
        public const string CannotReplyToReply = "error.review.cannot_reply_to_reply";
    }

    public static class Report
    {
        public const string NotFound = "error.report.not_found";
        public const string TargetNotFound = "error.report.target_not_found";
        public const string AlreadyProcessed = "error.report.already_processed";
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
