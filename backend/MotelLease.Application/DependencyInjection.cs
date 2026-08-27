using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Accounts;
using MotelLease.Application.Appointments;
using MotelLease.Application.Auth;
using MotelLease.Application.Bills;
using MotelLease.Application.BoardingHouses;
using MotelLease.Application.Catalogue;
using MotelLease.Application.Common.Security;
using MotelLease.Application.Deposits;
using MotelLease.Application.Extensions;
using MotelLease.Application.Leases;
using MotelLease.Application.Notifications;
using MotelLease.Application.Payments;
using MotelLease.Application.Rooms;
using MotelLease.Application.RoomTypes;
using MotelLease.Application.SavedListings;

namespace MotelLease.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Handlers are scoped: each one holds the request's IAppDbContext and ICurrentUser.
    /// Registered explicitly rather than by assembly scan, so an unreferenced handler shows
    /// up as a missing registration instead of silently resolving.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterHandler>(ServiceLifetime.Singleton);

        services.AddScoped<SessionIssuer>();
        services.AddScoped<OtpDispatcher>();
        services.AddSingleton(VerifiedEmailWindow.Default);

        services.AddScoped<SendRegistrationOtpHandler>();
        services.AddScoped<VerifyRegistrationOtpHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<LoginWithGoogleHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();

        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
        services.AddScoped<UpdateLanguageHandler>();
        services.AddScoped<UpdateAvatarHandler>();
        services.AddScoped<SendEmailChangeOtpHandler>();
        services.AddScoped<VerifyEmailChangeOtpHandler>();
        services.AddScoped<GetSessionsHandler>();
        services.AddScoped<RevokeSessionHandler>();

        services.AddScoped<BoardingHouseAccess>();

        services.AddScoped<ListMyBoardingHousesHandler>();
        services.AddScoped<GetBoardingHouseHandler>();
        services.AddScoped<CreateBoardingHouseHandler>();
        services.AddScoped<UpdateBoardingHouseHandler>();
        services.AddScoped<DeleteBoardingHouseHandler>();
        services.AddScoped<SubmitBoardingHouseForReviewHandler>();
        services.AddScoped<UpdateUtilityPricesHandler>();
        services.AddScoped<AddBoardingHouseImageHandler>();
        services.AddScoped<DeleteBoardingHouseImageHandler>();
        services.AddScoped<SetPrimaryBoardingHouseImageHandler>();

        services.AddScoped<ListRoomTypesHandler>();
        services.AddScoped<CreateRoomTypeHandler>();
        services.AddScoped<UpdateRoomTypeHandler>();
        services.AddScoped<DeleteRoomTypeHandler>();

        services.AddScoped<ListRoomsHandler>();
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<UpdateRoomHandler>();
        services.AddScoped<DeleteRoomHandler>();
        services.AddScoped<UpdateRoomStatusHandler>();
        services.AddScoped<UpdateMeterReadingsHandler>();

        services.AddScoped<NotificationDispatcher>();

        services.AddScoped<ListAppointmentsHandler>();
        services.AddScoped<GetAppointmentHandler>();
        services.AddScoped<BookAppointmentHandler>();
        services.AddScoped<AnswerAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<ExpirePastAppointmentsHandler>();

        services.AddSingleton(DepositPaymentWindow.Default);

        services.AddScoped<ListDepositsHandler>();
        services.AddScoped<GetDepositHandler>();
        services.AddScoped<RequestDepositHandler>();
        services.AddScoped<AnswerDepositHandler>();
        services.AddScoped<CancelDepositHandler>();
        services.AddScoped<PreviewDepositContractHandler>();
        services.AddScoped<ExpireOverdueDepositsHandler>();

        services.AddSingleton(PaymentSessionWindow.Default);
        services.AddSingleton<PaymentGateways>();

        services.AddScoped<StartDepositPaymentHandler>();
        services.AddScoped<StartBillPaymentHandler>();
        services.AddScoped<ConfirmPaymentHandler>();
        services.AddScoped<ReadPaymentReturnHandler>();
        services.AddScoped<ListPaymentsHandler>();
        services.AddScoped<GetPaymentHandler>();

        services.AddScoped<ConfirmDepositLeaseHandler>();
        services.AddScoped<ListLeasesHandler>();
        services.AddScoped<GetLeaseHandler>();
        services.AddScoped<GetCurrentLeaseHandler>();
        services.AddScoped<GetRoomLeaseHistoryHandler>();
        services.AddScoped<AddLeaseTenantHandler>();
        services.AddScoped<RemoveLeaseTenantHandler>();
        services.AddScoped<PreviewLeaseTerminationHandler>();
        services.AddScoped<TerminateLeaseHandler>();
        services.AddScoped<SweepLeaseExpiryHandler>();

        services.AddScoped<ListExtensionRequestsHandler>();
        services.AddScoped<GetExtensionRequestHandler>();
        services.AddScoped<CreateExtensionRequestHandler>();
        services.AddScoped<ApproveExtensionRequestHandler>();
        services.AddScoped<RejectExtensionRequestHandler>();

        services.AddScoped<ListRoomAdditionalFeesHandler>();
        services.AddScoped<CreateRoomAdditionalFeeHandler>();
        services.AddScoped<UpdateRoomAdditionalFeeHandler>();
        services.AddScoped<DeleteRoomAdditionalFeeHandler>();

        services.AddScoped<PreviewBillHandler>();
        services.AddScoped<CreateBillHandler>();
        services.AddScoped<ListBillsHandler>();
        services.AddScoped<GetBillHandler>();
        services.AddScoped<UpdateDraftBillHandler>();
        services.AddScoped<IssueDraftBillHandler>();
        services.AddScoped<CancelBillHandler>();
        services.AddScoped<GenerateBillPdfHandler>();
        services.AddScoped<SweepBillRemindersHandler>();

        services.AddScoped<ListNotificationsHandler>();
        services.AddScoped<CountUnreadNotificationsHandler>();
        services.AddScoped<MarkNotificationReadHandler>();
        services.AddScoped<MarkAllNotificationsReadHandler>();

        services.AddScoped<SearchBoardingHousesHandler>();
        services.AddScoped<GetNearbyBoardingHousesHandler>();
        services.AddScoped<GetMapBoardingHousesHandler>();
        services.AddScoped<GetBoardingHouseDetailHandler>();
        services.AddScoped<GetBoardingHouseVacantRoomsHandler>();
        services.AddScoped<GetBoardingHouseReviewsHandler>();
        services.AddScoped<ListFacilitiesHandler>();
        services.AddScoped<ListProvincesHandler>();
        services.AddScoped<ListDistrictsHandler>();

        services.AddScoped<ListSavedListingsHandler>();
        services.AddScoped<SaveListingHandler>();
        services.AddScoped<RemoveSavedListingHandler>();

        return services;
    }
}
