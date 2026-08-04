import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'core/auth/auth_provider.dart';
import 'features/auth/client_login_screen.dart';
import 'features/auth/lawyer_login_screen.dart';
import 'features/auth/role_select_screen.dart';
import 'features/home/home_screen.dart';
import 'features/lawyers/lawyer_directory_screen.dart';
import 'features/lawyer_requests/lawyer_requests_screen.dart';
import 'features/orders/orders_screen.dart';

/// Route guard mirrors both web apps' RequireAuth — an unauthenticated visit to any
/// authenticated route redirects to the role-select screen instead of a 401.
final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/',
    refreshListenable: _AuthListenable(ref),
    redirect: (context, state) {
      final auth = ref.read(authProvider);
      if (auth.isLoading) return null;

      final isAuthRoute = state.matchedLocation.startsWith('/login');
      if (!auth.isAuthenticated && !isAuthRoute) return '/login';
      if (auth.isAuthenticated && isAuthRoute) return '/';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const RoleSelectScreen()),
      GoRoute(path: '/login/client', builder: (context, state) => const ClientLoginScreen()),
      GoRoute(path: '/login/lawyer', builder: (context, state) => const LawyerLoginScreen()),
      GoRoute(path: '/', builder: (context, state) => const HomeScreen()),
      GoRoute(path: '/lawyers', builder: (context, state) => const LawyerDirectoryScreen()),
      GoRoute(path: '/orders', builder: (context, state) => const OrdersScreen()),
      GoRoute(path: '/requests', builder: (context, state) => const LawyerRequestsScreen()),
    ],
  );
});

class _AuthListenable extends ChangeNotifier {
  _AuthListenable(this.ref) {
    ref.listen(authProvider, (_, _) => notifyListeners());
  }
  final Ref ref;
}
