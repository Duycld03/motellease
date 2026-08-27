using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Payments;

/// <summary>
/// How long a checkout stays payable at the gateway. Much shorter than a deposit's payment deadline
/// on purpose: the deadline is what the owner is holding the room for, while an abandoned checkout
/// should stop being payable quickly so a late payment cannot land against an attempt everyone has
/// forgotten. A tenant who runs out of time starts a new attempt.
/// </summary>
public sealed record PaymentSessionWindow(TimeSpan Lifetime)
{
    public static readonly PaymentSessionWindow Default = new(TimeSpan.FromMinutes(15));
}

/// <summary>
/// The gateways this deployment can use, looked up by provider. Registered as the set rather than as
/// one implementation so adding MoMo is a registration and nothing else.
/// </summary>
public sealed class PaymentGateways(IEnumerable<IPaymentGateway> gateways)
{
    private readonly Dictionary<PaymentProvider, IPaymentGateway> _byProvider =
        gateways.ToDictionary(g => g.Provider);

    public IPaymentGateway For(PaymentProvider provider) =>
        _byProvider.TryGetValue(provider, out var gateway)
            ? gateway
            : throw new BusinessRuleException(MessageKeys.Payment.ProviderNotAvailable);

    public bool Supports(PaymentProvider provider) => _byProvider.ContainsKey(provider);
}
