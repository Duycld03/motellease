using MotelLease.Application.Bills.Contracts;

namespace MotelLease.Application.Common.Abstractions;

public interface IBillPdfGenerator
{
    byte[] Generate(BillResponse bill, string language);
}
