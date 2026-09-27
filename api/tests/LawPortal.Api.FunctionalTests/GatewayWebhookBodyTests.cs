using System.Text.Json;
using LawPortal.Api.Controllers.V1.Public;

namespace LawPortal.Api.FunctionalTests;

/// <summary>
/// The webhook body is the one place where a silent mismatch costs real money: if the reference we
/// pull out of it doesn't match what we stored at checkout, the lookup misses, nothing settles, and
/// the payer is charged while the order sits at Submitted. The subtlety is that Moyasar never sends
/// an invoice-level event — only payment_* ones — so the payment id in the body is NOT the id we
/// stored (we stored the invoice id). data.invoice_id is the only thing that bridges them.
///
/// Deserializes real JSON rather than constructing the record directly, because the thing most
/// likely to rot is the [JsonPropertyName("invoice_id")] mapping, and constructing by hand would
/// skip straight past it.
/// </summary>
public class GatewayWebhookBodyTests
{
    // Matches what ASP.NET Core's default input formatter uses.
    private static readonly JsonSerializerOptions Options = JsonSerializerOptions.Web;

    private static WebhooksController.GatewayWebhookBody Parse(string json) =>
        JsonSerializer.Deserialize<WebhooksController.GatewayWebhookBody>(json, Options)!;

    [Fact]
    public void MoyasarPaymentPaid_ResolvesToTheInvoiceId_NotTheEventOrPaymentId()
    {
        // Shape per Moyasar's webhook reference: top-level id is the EVENT id.
        var body = Parse("""
        {
          "id": "d4f5e6a7-0000-4000-8000-eventideventid",
          "type": "payment_paid",
          "created_at": "2026-09-13T10:00:00Z",
          "secret_token": "dev_only_webhook_secret_replace_before_launch",
          "account_name": "Law Portal",
          "live": false,
          "data": {
            "id": "aaaaaaaa-1111-4111-8111-paymentidhere",
            "status": "paid",
            "amount": 34500,
            "currency": "SAR",
            "invoice_id": "bbbbbbbb-2222-4222-8222-invoiceidhere"
          }
        }
        """);

        // This is the assertion that matters — the invoice id is what Payment.GatewayPaymentId holds.
        Assert.Equal("bbbbbbbb-2222-4222-8222-invoiceidhere", body.GatewayReference);
        Assert.Equal("paid", body.ResolvedStatus);
        Assert.Equal("dev_only_webhook_secret_replace_before_launch", body.SecretToken);
    }

    [Fact]
    public void MoyasarPaymentFailed_ResolvesTheNestedStatus()
    {
        var body = Parse("""
        {
          "id": "event-id",
          "type": "payment_failed",
          "secret_token": "s",
          "data": { "id": "pay-id", "status": "failed", "invoice_id": "inv-id" }
        }
        """);

        Assert.Equal("inv-id", body.GatewayReference);
        Assert.Equal("failed", body.ResolvedStatus);
    }

    [Fact]
    public void FakeGatewayFlatBody_StillResolves()
    {
        // FakeGatewayController posts this shape — no "data" at all. It must keep working, since
        // it's the offline dev path that exercises the same ledger code with no Moyasar account.
        var body = Parse("""
        {
          "id": "cccccccc-3333-4333-8333-ourpaymentid",
          "status": "paid",
          "secret_token": "dev_only_webhook_secret_replace_before_launch"
        }
        """);

        Assert.Equal("cccccccc-3333-4333-8333-ourpaymentid", body.GatewayReference);
        Assert.Equal("paid", body.ResolvedStatus);
    }

    [Fact]
    public void PaymentWithoutAnInvoice_FallsBackToThePaymentId()
    {
        // Not a path we create today (we always open a hosted invoice), but a direct payment
        // carries no invoice_id, and falling back beats resolving to the event id.
        var body = Parse("""
        {
          "id": "event-id",
          "type": "payment_paid",
          "secret_token": "s",
          "data": { "id": "pay-id", "status": "paid", "invoice_id": null }
        }
        """);

        Assert.Equal("pay-id", body.GatewayReference);
    }

    [Fact]
    public void UnrecognisedBody_ResolvesToNull_SoTheControllerCanRejectIt()
    {
        var body = Parse("""{ "secret_token": "s" }""");

        Assert.Null(body.GatewayReference);
        Assert.Null(body.ResolvedStatus);
    }
}
