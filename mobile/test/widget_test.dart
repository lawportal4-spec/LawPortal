import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:law_portal/main.dart';

void main() {
  testWidgets('Portal home renders the five service categories', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: LawPortalApp()));
    await tester.pumpAndSettle();

    expect(find.text('الاستشارات القانونية'), findsOneWidget);
    expect(find.text('القضاء والتنفيذ'), findsOneWidget);
  });
}
