import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router-dom";
import { Wallet as WalletIcon } from "lucide-react";
import { Button, Card, Input, SectionHeading, StatusTag, Ltr } from "@law-portal/ui";
import { useTranslation, formatCurrency, formatDateTime } from "@law-portal/i18n";
import { AppShell } from "../components/AppShell";
import { getWallet, getWalletTransactions, topUpWallet } from "../lib/paymentsApi";

/** Same gap OrderDetail polls across — the gateway redirects back before its webhook lands.
 * Shorter here than on an order, because the balance on screen is already live-updating from the
 * poll; all this bounds is how long the "confirming" note stays up. */
const GATEWAY_POLL_TIMEOUT_MS = 30_000;

export default function Wallet() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const [amount, setAmount] = useState("");
  const [error, setError] = useState<string | null>(null);
  // PaymentsReturnController sends a wallet top-up back here with ?payment=returned.
  const [awaitingGateway, setAwaitingGateway] = useState(searchParams.get("payment") === "returned");

  const walletQuery = useQuery({
    queryKey: ["wallet"],
    queryFn: getWallet,
    refetchInterval: awaitingGateway ? 2000 : false,
  });

  const transactionsQuery = useQuery({
    queryKey: ["walletTransactions"],
    queryFn: getWalletTransactions,
    refetchInterval: awaitingGateway ? 2000 : false,
  });

  // A top-up credits the balance only once the webhook arrives, so the balance moving is the
  // signal it landed — nothing else on this page can change it while we wait. The baseline has to
  // be the first *loaded* balance, not whatever we have on the first render: coming back from the
  // gateway the query is still undefined then, and treating that as the baseline would read the
  // very first successful load as "it changed" and stop polling instantly.
  const balance = walletQuery.data?.balance;
  const baselineBalance = useRef<number | undefined>(undefined);
  useEffect(() => {
    if (!awaitingGateway) {
      baselineBalance.current = undefined;
      return;
    }
    if (balance === undefined) return;
    if (baselineBalance.current === undefined) {
      baselineBalance.current = balance;
      return;
    }
    if (balance !== baselineBalance.current) setAwaitingGateway(false);
  }, [awaitingGateway, balance]);

  // If the webhook had already landed before this page finished loading, the baseline above is
  // already the credited balance and nothing will ever look like a change — so cap the wait.
  useEffect(() => {
    if (!awaitingGateway) return;
    const timer = setTimeout(() => setAwaitingGateway(false), GATEWAY_POLL_TIMEOUT_MS);
    return () => clearTimeout(timer);
  }, [awaitingGateway]);

  const topUpMutation = useMutation({
    mutationFn: (sar: number) => topUpWallet(sar),
    onSuccess: (result) => {
      setError(null);
      if (result.redirectUrl) {
        setAwaitingGateway(true);
        window.location.href = result.redirectUrl;
      }
    },
    onError: () => setError(t("wallet.topUpFailed")),
  });

  function submitTopUp() {
    const sar = Number(amount);
    if (!Number.isFinite(sar) || sar <= 0) {
      setError(t("wallet.invalidAmount"));
      return;
    }
    topUpMutation.mutate(sar);
  }

  function refreshAfterGateway() {
    setAwaitingGateway(false);
    void queryClient.invalidateQueries({ queryKey: ["wallet"] });
    void queryClient.invalidateQueries({ queryKey: ["walletTransactions"] });
  }

  return (
    <AppShell>
      <div className="mx-auto max-w-2xl">
        <SectionHeading level={2} className="mb-4">
          {t("wallet.title")}
        </SectionHeading>

        {walletQuery.isError && <p className="text-sm text-rubric">{t("wallet.loadFailed")}</p>}

        <Card elevated className="mb-4">
          <p className="text-sm text-ink-faint">{t("wallet.balance")}</p>
          <p className="mt-1 flex items-center gap-2 text-3xl font-bold text-ink">
            <WalletIcon className="h-6 w-6 text-seal" />
            <Ltr className="font-mono">
              {walletQuery.data ? formatCurrency(walletQuery.data.balance) : "—"}
            </Ltr>
          </p>
        </Card>

        <Card className="mb-4">
          <SectionHeading level={3} className="mb-1">
            {t("wallet.topUp")}
          </SectionHeading>
          <p className="mb-3 text-sm text-ink-faint">{t("wallet.topUpHint")}</p>

          {awaitingGateway ? (
            <div className="flex flex-col gap-3">
              <p className="text-sm text-ink-soft">{t("wallet.confirming")}</p>
              <Button variant="secondary" onClick={refreshAfterGateway}>
                {t("wallet.checkStatus")}
              </Button>
            </div>
          ) : (
            <div className="flex flex-wrap items-center gap-3">
              <Input
                type="number"
                min="1"
                step="1"
                inputMode="decimal"
                className="flex-1"
                aria-label={t("wallet.topUpAmount")}
                placeholder={t("wallet.amountPlaceholder")}
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
              <Button onClick={submitTopUp} disabled={topUpMutation.isPending}>
                {t("wallet.topUpAction")}
              </Button>
            </div>
          )}
          {error && <p className="mt-2 text-sm text-rubric">{error}</p>}
        </Card>

        <Card>
          <SectionHeading level={3} className="mb-3">
            {t("wallet.transactions")}
          </SectionHeading>
          {transactionsQuery.data?.length ? (
            <div className="flex flex-col">
              {transactionsQuery.data.map((tx) => (
                <div
                  key={tx.id}
                  className="flex items-center justify-between gap-3 border-b border-border py-3 text-sm last:border-0"
                >
                  <div className="flex flex-col gap-1">
                    <span className="font-medium text-ink">{tx.description ?? t(`wallet.type.${tx.type}`)}</span>
                    <Ltr className="font-mono text-xs text-ink-faint">{formatDateTime(tx.createdAtUtc)}</Ltr>
                  </div>
                  <div className="flex items-center gap-2">
                    <StatusTag status={tx.type} label={t(`wallet.type.${tx.type}`)} />
                    <Ltr className="font-mono font-medium text-ink">{formatCurrency(tx.amount)}</Ltr>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-ink-faint">{t("wallet.noTransactions")}</p>
          )}
        </Card>
      </div>
    </AppShell>
  );
}
