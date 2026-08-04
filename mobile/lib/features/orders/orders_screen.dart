import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/i18n/format.dart';
import '../../core/i18n/locale_provider.dart';
import '../../core/theme/tokens.dart';
import 'orders_api.dart';

final myOrdersProvider = FutureProvider.autoDispose((ref) => ref.read(ordersApiProvider).listMyRequests());

const _statusLabelsAr = {
  'Draft': 'مسودة',
  'Submitted': 'مُقدَّم',
  'Paid': 'مدفوع',
  'InProgress': 'قيد التنفيذ',
  'Completed': 'مكتمل',
  'Cancelled': 'ملغى',
  'Refunded': 'مُسترجَع',
};

class OrdersScreen extends ConsumerWidget {
  const OrdersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isAr = isArabic(ref.watch(localeProvider));
    final orders = ref.watch(myOrdersProvider);

    return Directionality(
      textDirection: isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        appBar: AppBar(title: Text(isAr ? 'طلباتي' : 'My Orders')),
        body: orders.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (err, _) => Center(child: Text(isAr ? 'تعذّر تحميل الطلبات.' : 'Could not load your orders.')),
          data: (items) => items.isEmpty
              ? Center(child: Text(isAr ? 'لا توجد طلبات بعد.' : 'No orders yet.'))
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 10),
                  itemBuilder: (context, i) {
                    final o = items[i];
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
                                Text(isAr ? o.serviceNameAr : o.serviceNameEn, style: const TextStyle(fontWeight: FontWeight.w600)),
                                Directionality(
                                  textDirection: TextDirection.ltr,
                                  child: Text(o.number, style: TextStyle(fontFamily: LpFonts.mono, fontSize: 12, color: LpColorsLight.inkFaint)),
                                ),
                              ],
                            ),
                          ),
                          if (o.subtotal != null)
                            Directionality(
                              textDirection: TextDirection.ltr,
                              child: Text(formatCurrency(o.subtotal!), style: TextStyle(fontFamily: LpFonts.mono, fontSize: 12)),
                            ),
                          const SizedBox(width: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                            decoration: BoxDecoration(color: LpColorsLight.sealTint, borderRadius: BorderRadius.circular(LpRadius.radiusFull)),
                            child: Text(
                              isAr ? (_statusLabelsAr[o.status] ?? o.status) : o.status,
                              style: TextStyle(color: LpColorsLight.sealStrong, fontSize: 11, fontWeight: FontWeight.w600),
                            ),
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
