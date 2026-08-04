import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../auth/auth_storage.dart';

/// One Dio instance for the whole app, matching how both web clients centralize their axios
/// instance — the interceptor reads the token fresh on every request rather than caching it in
/// a closure, avoiding the exact registration-order race P3 found and fixed on the web side.
final apiClientProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(baseUrl: 'http://localhost:5280'));
  final storage = AuthStorage();

  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) async {
      final token = await storage.readAccessToken();
      if (token != null) options.headers['Authorization'] = 'Bearer $token';
      handler.next(options);
    },
  ));

  return dio;
});
