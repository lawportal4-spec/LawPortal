import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../api/api_client.dart';
import 'auth_storage.dart';

enum LpUserType { client, lawyer }

class AuthState {
  const AuthState({this.userType, this.isLoading = true});
  final LpUserType? userType;
  final bool isLoading;

  bool get isAuthenticated => userType != null;
}

/// Resolved once at app start by reading whatever role was stored at the last successful
/// login — this is the "one app, both roles, role resolved after login" decision from the
/// plan, mirrored from the two separate web apps into a single client here.
class AuthNotifier extends StateNotifier<AuthState> {
  AuthNotifier(this._storage) : super(const AuthState()) {
    _restore();
  }

  final AuthStorage _storage;

  Future<void> _restore() async {
    final userType = await _storage.readUserType();
    state = AuthState(userType: userType == 'lawyer' ? LpUserType.lawyer : userType == 'client' ? LpUserType.client : null, isLoading: false);
  }

  Future<void> loginAsClient(String accessToken, String refreshToken) async {
    await _storage.save(accessToken: accessToken, refreshToken: refreshToken, userType: 'client');
    state = const AuthState(userType: LpUserType.client, isLoading: false);
  }

  Future<void> loginAsLawyer(String accessToken, String refreshToken) async {
    await _storage.save(accessToken: accessToken, refreshToken: refreshToken, userType: 'lawyer');
    state = const AuthState(userType: LpUserType.lawyer, isLoading: false);
  }

  Future<void> logout() async {
    await _storage.clear();
    state = const AuthState(userType: null, isLoading: false);
  }
}

final authStorageProvider = Provider((ref) => AuthStorage());

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier(ref.read(authStorageProvider));
});

class AuthApi {
  AuthApi(this._dio);
  final Dio _dio;

  Future<void> requestClientOtp(String phoneE164) async {
    await _dio.post('/api/v1/auth/client/otp/request', data: {
      'phoneE164': phoneE164,
      'recaptchaToken': 'dev-placeholder',
    });
  }

  Future<(String, String)> verifyClientOtp(String phoneE164, String code) async {
    final res = await _dio.post('/api/v1/auth/client/otp/verify', data: {
      'phoneE164': phoneE164,
      'code': code,
    });
    return (res.data['accessToken'] as String, res.data['refreshToken'] as String);
  }

  Future<(String, String)> lawyerLogin(String email, String password) async {
    final res = await _dio.post('/api/v1/auth/lawyer/login', data: {
      'email': email,
      'password': password,
      'recaptchaToken': 'dev-placeholder',
    });
    return (res.data['accessToken'] as String, res.data['refreshToken'] as String);
  }
}

final authApiProvider = Provider((ref) => AuthApi(ref.read(apiClientProvider)));
