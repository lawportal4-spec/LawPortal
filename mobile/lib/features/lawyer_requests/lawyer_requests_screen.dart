import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/i18n/format.dart';
import '../../core/i18n/locale_provider.dart';
import '../../core/theme/tokens.dart';
import 'lawyer_requests_api.dart';

final incomingRequestsProvider = FutureProvider.autoDispose((ref) => ref.read(lawyerRequestsApiProvider).listIncoming());

/// Mirrors web-lawyer's Requests.tsx + accept/complete actions — the same backend endpoints
/// (P6) driven from a third client now.
class LawyerRequestsScreen extends ConsumerWidget {
  const LawyerRequestsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isAr = isArabic(ref.watch(localeProvider));
    final requests = ref.watch(incomingRequestsProvider);

    return Directionality(
      textDirection: isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        appBar: AppBar(title: Text(isAr ? 'الطلبات الواردة' : 'Incoming Requests')),
        body: requests.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (err, _) => Center(child: Text(isAr ? 'تعذّر تحميل الطلبات.' : 'Could not load requests.')),
          data: (items) => items.isEmpty
              ? Center(child: Text(isAr ? 'لا توجد طلبات واردة.' : 'No incoming requests.'))
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 10),
                  itemBuilder: (context, i) {
                    final r = items[i];
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
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Text(isAr ? r.serviceNameAr : r.serviceNameEn, style: const TextStyle(fontWeight: FontWeight.w600)),
                              if (r.subtotal != null)
                                Directionality(
                                  textDirection: TextDirection.ltr,
                                  child: Text(formatCurrency(r.subtotal!), style: TextStyle(fontFamily: LpFonts.mono, fontSize: 12)),
                                ),
                            ],
                          ),
                          Text(r.clientName, style: TextStyle(color: LpColorsLight.inkFaint, fontSize: 12)),
                          const SizedBox(height: 10),
                          if (r.status == 'Paid')
                            FilledButton(
                              onPressed: () async {
                                await ref.read(lawyerRequestsApiProvider).accept(r.id);
                                ref.invalidate(incomingRequestsProvider);
                              },
                              child: Text(isAr ? 'قبول الطلب' : 'Accept'),
                            )
                          else if (r.status == 'InProgress')
                            FilledButton(
                              onPressed: () async {
                                await ref.read(lawyerRequestsApiProvider).complete(r.id);
                                ref.invalidate(incomingRequestsProvider);
                              },
                              child: Text(isAr ? 'تحديد كمكتمل' : 'Mark complete'),
                            )
                          else
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(color: LpColorsLight.sealTint, borderRadius: BorderRadius.circular(LpRadius.radiusFull)),
                              child: Text(r.status, style: TextStyle(color: LpColorsLight.sealStrong, fontSize: 11)),
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
