import 'package:intl/intl.dart';

/// Centralized formatting — the Flutter twin of packages/i18n/src/format.ts. `NumberFormat`
/// with locale 'ar' defaults to Arabic-Indic digits; pinning 'en' output for numerals (while
/// keeping Arabic labels around them) is what keeps a "1-3 years" range from ever reordering.
String formatNumber(num value) => NumberFormat.decimalPattern('en').format(value);

String formatCurrency(num amountSar, {bool showSymbol = true}) {
  final amount = NumberFormat('#,##0.00', 'en').format(amountSar);
  return showSymbol ? '$amount SAR' : amount;
}

String formatExperienceRange(int minYears, int? maxYears) {
  if (maxYears == null) return '${formatNumber(minYears)}+';
  return '${formatNumber(minYears)}-${formatNumber(maxYears)}';
}
