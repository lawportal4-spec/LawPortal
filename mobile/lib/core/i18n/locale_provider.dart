import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Owns the app's locale. Flutter derives Directionality from the locale automatically —
/// no manual dir="rtl" management needed, unlike the web (see @law-portal/i18n on the web side).
final localeProvider = StateProvider<String>((ref) => 'ar');

bool isArabic(String locale) => locale == 'ar';
