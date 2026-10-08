using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPayoutsAfterRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data fix only. Refunds used to reverse the lawyer's share in the ledger but leave the
            // payout untouched, so a later release would pay the full original share.
            // 1) Held payouts lose what their refunds took out of escrow (Account 2 = EscrowPayable, debit).
            migrationBuilder.Sql(@"
                UPDATE payouts o
                JOIN (
                    SELECT r.PaymentId, SUM(l.Amount) AS Reversed
                    FROM refunds r
                    JOIN ledger_entries l ON l.ReferenceType = 'Refund' AND l.ReferenceId = r.Id AND l.Account = 2 AND l.IsDebit = 1
                    GROUP BY r.PaymentId
                ) x ON x.PaymentId = o.PaymentId
                SET o.Amount = o.Amount - x.Reversed
                WHERE o.Status = 0;");
            // 2) A fully refunded payment (Status 30) owes the lawyer nothing: cancel its held payout (Status 20).
            migrationBuilder.Sql(@"
                UPDATE payouts o
                JOIN payments p ON p.Id = o.PaymentId
                SET o.Status = 20, o.Amount = 0
                WHERE p.Status = 30 AND o.Status = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: restoring the old amounts would bring back the over-payment.
        }
    }
}
