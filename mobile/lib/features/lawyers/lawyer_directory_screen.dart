import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/i18n/format.dart';
import '../../core/i18n/locale_provider.dart';
import '../../core/theme/tokens.dart';
import 'lawyers_api.dart';

final lawyerSearchProvider = FutureProvider.autoDispose((ref) => ref.read(lawyersApiProvider).search());

/// Proves the mobile client can drive the exact same `/api/v1/lawyers` endpoint the web
/// directory (P2) already verified — same server, same data, a different rendering surface.
class LawyerDirectoryScreen extends ConsumerWidget {
  const LawyerDirectoryScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isAr = isArabic(ref.watch(localeProvider));
    final lawyers = ref.watch(lawyerSearchProvider);

    return Directionality(
      textDirection: isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        appBar: AppBar(title: Text(isAr ? 'دليل المحامين' : 'Lawyer Directory')),
        body: lawyers.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (err, _) => Center(child: Text(isAr ? 'تعذّر تحميل الدليل.' : 'Could not load the directory.')),
          data: (items) => ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: items.length,
            separatorBuilder: (_, _) => const SizedBox(height: 10),
            itemBuilder: (context, i) {
              final l = items[i];
              return Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: LpColorsLight.surface,
                  border: Border.all(color: LpColorsLight.border),
                  borderRadius: BorderRadius.circular(LpRadius.radiusLg),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(children: [
                            Text(l.fullName, style: const TextStyle(fontWeight: FontWeight.w600)),
                            if (l.isVerified) ...[
                              const SizedBox(width: 4),
                              Icon(Icons.verified, size: 14, color: LpColorsLight.seal),
                            ],
                          ]),
                          Text(
                            isAr ? (l.cityNameAr ?? '—') : (l.cityNameEn ?? '—'),
                            style: TextStyle(color: LpColorsLight.inkFaint, fontSize: 12),
                          ),
                        ],
                      ),
                    ),
                    Directionality(
                      textDirection: TextDirection.ltr,
                      child: Text(formatCurrency(l.writtenPrice), style: TextStyle(fontFamily: LpFonts.mono, fontWeight: FontWeight.w600)),
                    ),
                  ],
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}
