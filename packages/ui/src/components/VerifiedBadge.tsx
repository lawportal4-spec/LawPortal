export function VerifiedBadge({ title = "Verified" }: { title?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className="h-4 w-4 flex-shrink-0 text-seal" role="img" aria-label={title}>
      <title>{title}</title>
      <path d="M12 2l2.4 2.2 3.2-.4 1 3 2.8 1.6-.8 3.2.8 3.2-2.8 1.6-1 3-3.2-.4L12 22l-2.4-2.2-3.2.4-1-3-2.8-1.6.8-3.2-.8-3.2 2.8-1.6 1-3 3.2.4L12 2z" />
      <path d="M9 12.5l2 2 4-4.5" stroke="var(--color-surface)" strokeWidth="1.6" fill="none" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
