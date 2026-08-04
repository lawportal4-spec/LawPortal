namespace LawPortal.Application.Payments.Dtos;

public record CheckoutResultDto(
    Guid PaymentId,
    string Number,
    string Status,
    string? RedirectUrl,
    bool PaidImmediately);

public record InvoiceDto(
    string Number,
    decimal SubtotalExVat,
    decimal VatAmount,
    decimal Total,
    bool IsVatApplicable,
    string SellerNameAr,
    string SellerNameEn,
    string? SellerVatNumber,
    string QrPayloadBase64,
    DateTime IssuedAtUtc);

public record PaymentSummaryDto(
    Guid Id,
    string Number,
    string Status,
    decimal Total,
    string CurrencyCode,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);

public record WalletDto(decimal Balance, string CurrencyCode);

public record WalletTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string? Description,
    DateTime CreatedAtUtc);
