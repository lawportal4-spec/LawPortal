import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/auth/auth_provider.dart';
import '../../core/i18n/locale_provider.dart';
import '../../core/i18n/format.dart';
import '../../core/theme/tokens.dart';

/// Mirrors apps/web-client/src/pages/PortalHome.tsx — same content, same design tokens,
/// so the two platforms can be reviewed side by side (see P0 exit criteria). Now role-gated:
/// the nav row below routes to the client or lawyer screens depending on which role signed in,
/// the mobile expression of "one app, both roles."
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeProvider);
    final isAr = isArabic(locale);
    final theme = Theme.of(context);
    final userType = ref.watch(authProvider).userType;

    return Directionality(
      textDirection: isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Header(isAr: isAr, ref: ref),
                const SizedBox(height: 16),
                Row(
                  children: [
                    if (userType == LpUserType.client) ...[
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => context.push('/lawyers'),
                          child: Text(isAr ? 'دليل المحامين' : 'Lawyer Directory'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => context.push('/orders'),
                          child: Text(isAr ? 'طلباتي' : 'My Orders'),
                        ),
                      ),
                    ] else if (userType == LpUserType.lawyer)
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => context.push('/requests'),
                          child: Text(isAr ? 'الطلبات الواردة' : 'Incoming Requests'),
                        ),
                      ),
                    const SizedBox(width: 8),
                    IconButton(
                      onPressed: () => ref.read(authProvider.notifier).logout(),
                      icon: const Icon(Icons.logout),
                      tooltip: isAr ? 'خروج' : 'Log out',
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                Text(
                  isAr ? 'كيف يمكننا مساعدتك؟' : 'How can we help?',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontFamily: LpFonts.display,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 14),
                _CategoryGrid(isAr: isAr),
                const SizedBox(height: 28),
                Text(
                  isAr ? 'بطاقة محامٍ في الدليل' : 'A lawyer directory card',
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 12),
                _LawyerCard(isAr: isAr),
                const SizedBox(height: 28),
                Text(
                  isAr ? 'حالات الطلب' : 'Request statuses',
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 12),
                _StatusRow(isAr: isAr),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.isAr, required this.ref});
  final bool isAr;
  final WidgetRef ref;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Row(
          crossAxisAlignment: CrossAxisAlignment.baseline,
          textBaseline: TextBaseline.alphabetic,
          children: [
            Text(
              isAr ? 'بوابة القانون' : 'Law Portal',
              style: const TextStyle(fontFamily: LpFonts.display, fontWeight: FontWeight.bold, fontSize: 20),
            ),
            const SizedBox(width: 8),
            Text(
              isAr ? 'Law Portal' : 'بوابة القانون',
              style: TextStyle(color: LpColorsLight.inkFaint, fontSize: 13),
            ),
          ],
        ),
        _LangToggle(isAr: isAr, ref: ref),
      ],
    );
  }
}

class _LangToggle extends StatelessWidget {
  const _LangToggle({required this.isAr, required this.ref});
  final bool isAr;
  final WidgetRef ref;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(
        color: LpColorsLight.surface,
        border: Border.all(color: LpColorsLight.border),
        borderRadius: BorderRadius.circular(LpRadius.radiusFull),
      ),
      child: Row(
        children: [
          _LangButton(label: 'العربية', selected: isAr, onTap: () => ref.read(localeProvider.notifier).state = 'ar'),
          _LangButton(label: 'English', selected: !isAr, onTap: () => ref.read(localeProvider.notifier).state = 'en'),
        ],
      ),
    );
  }
}

class _LangButton extends StatelessWidget {
  const _LangButton({required this.label, required this.selected, required this.onTap});
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: selected ? LpColorsLight.seal : Colors.transparent,
          borderRadius: BorderRadius.circular(LpRadius.radiusFull),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: selected ? LpColorsLight.sealOn : LpColorsLight.inkSoft,
            fontWeight: FontWeight.w500,
            fontSize: 13,
          ),
        ),
      ),
    );
  }
}

class _CategoryGrid extends StatelessWidget {
  const _CategoryGrid({required this.isAr});
  final bool isAr;

  static const _categories = [
    (bg: LpColorsLight.catConsultBg, fg: LpColorsLight.catConsultFg, icon: Icons.balance, ar: 'الاستشارات القانونية', en: 'Legal Consultations'),
    (bg: LpColorsLight.catJudiciaryBg, fg: LpColorsLight.catJudiciaryFg, icon: Icons.gavel, ar: 'القضاء والتنفيذ', en: 'Judiciary & Execution'),
    (bg: LpColorsLight.catNotaryBg, fg: LpColorsLight.catNotaryFg, icon: Icons.description, ar: 'خدمات التوثيق', en: 'Notarization'),
    (bg: LpColorsLight.catBusinessBg, fg: LpColorsLight.catBusinessFg, icon: Icons.business_center, ar: 'خدمات الأعمال', en: 'Business Services'),
    (bg: LpColorsLight.catOtherBg, fg: LpColorsLight.catOtherFg, icon: Icons.folder_open, ar: 'خدمات أخرى', en: 'Other Services'),
  ];

  @override
  Widget build(BuildContext context) {
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        mainAxisSpacing: 10,
        crossAxisSpacing: 10,
        childAspectRatio: 1.5,
      ),
      itemCount: _categories.length,
      itemBuilder: (context, i) {
        final c = _categories[i];
        return Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: LpColorsLight.surface,
            border: Border.all(color: LpColorsLight.border),
            borderRadius: BorderRadius.circular(LpRadius.radiusLg),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(color: c.bg, borderRadius: BorderRadius.circular(LpRadius.radiusMd)),
                child: Icon(c.icon, size: 18, color: c.fg),
              ),
              const Spacer(),
              Text(isAr ? c.ar : c.en, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13)),
            ],
          ),
        );
      },
    );
  }
}

class _LawyerCard extends StatelessWidget {
  const _LawyerCard({required this.isAr});
  final bool isAr;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: LpColorsLight.surface,
        border: Border.all(color: LpColorsLight.border),
        borderRadius: BorderRadius.circular(LpRadius.radiusLg),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                backgroundColor: LpColorsLight.sealTint,
                child: Text(isAr ? 'س.ش' : 'SA', style: TextStyle(color: LpColorsLight.sealStrong, fontWeight: FontWeight.w600)),
              ),
              const SizedBox(width: 10),
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(isAr ? 'سارة الشهري' : 'Sarah Al-Shehri', style: const TextStyle(fontWeight: FontWeight.w600)),
                      const SizedBox(width: 4),
                      Icon(Icons.verified, size: 15, color: LpColorsLight.seal),
                    ],
                  ),
                  Text(isAr ? 'الرياض' : 'Riyadh', style: TextStyle(color: LpColorsLight.inkFaint, fontSize: 12)),
                ],
              ),
            ],
          ),
          const SizedBox(height: 10),
          // Credential chip — the signature element, at its inline scale.
          Container(
            padding: const EdgeInsets.fromLTRB(6, 6, 12, 6),
            decoration: BoxDecoration(
              color: LpColorsLight.vellum,
              border: Border.all(color: LpColorsLight.vellumLine),
              borderRadius: BorderRadius.circular(LpRadius.radiusFull),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircleAvatar(
                  radius: 10,
                  backgroundColor: LpColorsLight.vellum,
                  child: Text('ق', style: TextStyle(fontFamily: LpFonts.display, fontWeight: FontWeight.bold, fontSize: 11, color: LpColorsLight.vellumInk)),
                ),
                const SizedBox(width: 6),
                Text(isAr ? 'مرخّص' : 'Licensed', style: TextStyle(fontWeight: FontWeight.w600, fontSize: 12, color: LpColorsLight.vellumInk)),
                const SizedBox(width: 6),
                Directionality(
                  textDirection: TextDirection.ltr,
                  child: Text('472639', style: TextStyle(fontFamily: LpFonts.mono, fontSize: 12, color: LpColorsLight.vellumInk)),
                ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          Divider(color: LpColorsLight.rule),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(isAr ? 'استشارة كتابية من' : 'Written consult, from', style: TextStyle(fontSize: 11, color: LpColorsLight.inkFaint)),
                  Directionality(
                    textDirection: TextDirection.ltr,
                    child: Text(formatCurrency(220), style: TextStyle(fontFamily: LpFonts.mono, fontWeight: FontWeight.w600)),
                  ),
                ],
              ),
              ElevatedButton(onPressed: () {}, child: Text(isAr ? 'استشر' : 'Consult')),
            ],
          ),
        ],
      ),
    );
  }
}

class _StatusRow extends StatelessWidget {
  const _StatusRow({required this.isAr});
  final bool isAr;

  @override
  Widget build(BuildContext context) {
    final statuses = [
      (ar: 'مسودة', en: 'Draft', bg: LpColorsLight.surface, fg: LpColorsLight.inkFaint),
      (ar: 'بانتظار الدفع', en: 'Pending payment', bg: LpColorsLight.warningTint, fg: LpColorsLight.warning),
      (ar: 'قيد التنفيذ', en: 'In progress', bg: LpColorsLight.sealTint, fg: LpColorsLight.sealStrong),
      (ar: 'مكتمل', en: 'Completed', bg: LpColorsLight.seal, fg: LpColorsLight.sealOn),
      (ar: 'نزاع', en: 'Disputed', bg: LpColorsLight.rubricTint, fg: LpColorsLight.rubric),
    ];
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: statuses
          .map((s) => Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(color: s.bg, borderRadius: BorderRadius.circular(LpRadius.radiusFull)),
                child: Text(isAr ? s.ar : s.en, style: TextStyle(color: s.fg, fontSize: 12, fontWeight: FontWeight.w500)),
              ))
          .toList(),
    );
  }
}
