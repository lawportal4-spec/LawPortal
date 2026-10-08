using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LawPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecoverPayoutOverpayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data fix only. Before the refund fix, a payout refunded in part and then released paid the
            // lawyer their full original share. Record the excess as a debt (deducted from later payouts)
            // and put it back into escrow in the ledger, so escrow equals the held payouts again.
            migrationBuilder.Sql(@"
                INSERT INTO lawyer_debt_entries (Id, LawyerProfileId, Kind, Amount, PaymentId, PayoutId, Note, CreatedAtUtc)
                SELECT UUID(), o.LawyerProfileId, 1, ROUND(o.Amount - (p.NetToLawyerAmount - COALESCE(x.Reversed, 0)), 2),
                       o.PaymentId, o.Id, 'OverpaidBeforeRefundFix', UTC_TIMESTAMP()
                FROM payouts o
                JOIN payments p ON p.Id = o.PaymentId
                LEFT JOIN (
                    SELECT r.PaymentId, SUM(l.Amount) AS Reversed
                    FROM refunds r
                    JOIN ledger_entries l ON l.ReferenceType = 'Refund' AND l.ReferenceId = r.Id AND l.Account = 2 AND l.IsDebit = 1
                    GROUP BY r.PaymentId
                ) x ON x.PaymentId = o.PaymentId
                WHERE o.Status = 10 AND o.Amount - (p.NetToLawyerAmount - COALESCE(x.Reversed, 0)) > 0.005;");
            migrationBuilder.Sql(@"
                INSERT INTO ledger_entries (Id, Account, IsDebit, Amount, CurrencyCode, ReferenceType, ReferenceId, Description, CreatedAtUtc)
                SELECT UUID(), 9, 1, d.Amount, 'SAR', 'LawyerDebtCorrection', d.Id, 'Overpaid payout recovered as a debt', UTC_TIMESTAMP()
                FROM lawyer_debt_entries d WHERE d.Note = 'OverpaidBeforeRefundFix';");
            migrationBuilder.Sql(@"
                INSERT INTO ledger_entries (Id, Account, IsDebit, Amount, CurrencyCode, ReferenceType, ReferenceId, Description, CreatedAtUtc)
                SELECT UUID(), 2, 0, d.Amount, 'SAR', 'LawyerDebtCorrection', d.Id, 'Escrow restored for an overpaid payout', UTC_TIMESTAMP()
                FROM lawyer_debt_entries d WHERE d.Note = 'OverpaidBeforeRefundFix';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: the recovered amounts may already have been deducted.
        }
    }
}
