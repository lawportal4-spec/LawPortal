import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/i18n/locale_provider.dart';
import '../../core/theme/tokens.dart';

/// The mobile analogue of picking which of the two web apps to open — one binary, both roles,
/// resolved here rather than at install time (the plan's own "one app, both roles" decision).
class RoleSelectScreen extends ConsumerWidget {
  const RoleSelectScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isAr = isArabic(ref.watch(localeProvider));

    return Directionality(
      textDirection: isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  isAr ? 'بوابة القانون' : 'Law Portal',
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontFamily: LpFonts.display, fontWeight: FontWeight.bold, fontSize: 28),
                ),
                const SizedBox(height: 8),
                Text(
                  isAr ? 'كيف تريد الدخول؟' : 'How would you like to sign in?',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: LpColorsLight.inkFaint),
                ),
                const SizedBox(height: 32),
                FilledButton(
                  onPressed: () => context.go('/login/client'),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: Text(isAr ? 'الدخول كعميل' : 'Sign in as a client'),
                  ),
                ),
                const SizedBox(height: 12),
                OutlinedButton(
                  onPressed: () => context.go('/login/lawyer'),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 12),
                    child: Text(isAr ? 'الدخول كمحامٍ' : 'Sign in as a lawyer'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
