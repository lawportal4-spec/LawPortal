import { Link } from "react-router-dom";
import { Star } from "lucide-react";
import { Avatar, Button, Card, Chip, CredentialChipMini, Ltr, VerifiedBadge } from "@law-portal/ui";
import { formatCurrency, useTranslation } from "@law-portal/i18n";
import type { LawyerCardDto } from "../lib/api";

export function LawyerCard({ lawyer }: { lawyer: LawyerCardDto }) {
  const { i18n } = useTranslation();
  const isAr = i18n.language === "ar";
  const initials = initialsOf(lawyer.fullName);
  const specialties = isAr ? lawyer.specialtyNamesAr : lawyer.specialtyNamesEn;

  return (
    <Card className="flex h-full flex-col">
      <div className="flex items-start gap-3">
        <Avatar initials={initials} src={lawyer.photoUrl} online />
        <div>
          <div className="flex items-center gap-1.5 text-sm font-semibold">
            <Link to={`/lawyers/${lawyer.slug}`} className="hover:text-seal">
              {lawyer.fullName}
            </Link>
            {lawyer.isVerified && <VerifiedBadge />}
          </div>
          <div className="text-xs text-ink-faint">{isAr ? lawyer.cityNameAr : lawyer.cityNameEn}</div>
        </div>
      </div>

      <div className="mt-3">
        <CredentialChipMini
          licenceNumber={lawyer.licenseNumber}
          labelAr="مرخّص"
          labelEn="Licensed"
          locale={isAr ? "ar" : "en"}
        />
      </div>

      {specialties.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-1.5">
          {specialties.slice(0, 2).map((s) => (
            <Chip key={s}>{s}</Chip>
          ))}
        </div>
      )}

      <div className="mt-3 flex items-center gap-3 text-sm">
        {lawyer.avgRating != null ? (
          <span className="flex items-center gap-1.5">
            <Star className="h-4 w-4 text-warning" fill="currentColor" />
            <Ltr className="font-mono font-medium">{lawyer.avgRating.toFixed(1)}</Ltr>
            <Ltr className="font-mono text-ink-faint">({lawyer.ratingCount})</Ltr>
          </span>
        ) : (
          <span className="text-xs text-ink-faint">{isAr ? "انضم حديثًا" : "Newly joined"}</span>
        )}
        {lawyer.experienceDisplay && (
          <span className="text-xs text-ink-faint">
            · <Ltr className="font-mono">{lawyer.experienceDisplay}</Ltr> {isAr ? "سنوات خبرة" : "yrs exp."}
          </span>
        )}
      </div>

      <div className="mt-auto flex items-center justify-between border-t border-rule pt-4 mt-4">
        <div>
          <div className="text-xs text-ink-faint">{isAr ? "استشارة كتابية من" : "Written consult, from"}</div>
          <Ltr className="font-mono font-semibold">{formatCurrency(lawyer.writtenPrice)}</Ltr>
          <span
            className={
              "mt-1 inline-block rounded-full px-2 py-0.5 text-[0.66rem] " +
              (lawyer.isVatRegistered ? "bg-seal-tint text-seal-strong" : "bg-warning-tint text-warning")
            }
          >
            {lawyer.isVatRegistered
              ? isAr ? "مسجّل بالضريبة" : "VAT-registered"
              : isAr ? "غير مسجّل بالضريبة" : "Not VAT-registered"}
          </span>
        </div>
        <Link to={`/lawyers/${lawyer.slug}`}>
          <Button size="sm">{isAr ? "استشر" : "Consult"}</Button>
        </Link>
      </div>
    </Card>
  );
}

function initialsOf(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  return parts.length >= 2 ? `${parts[0][0]}.${parts[1][0]}` : fullName.slice(0, 2);
}
