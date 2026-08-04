import 'package:shared_preferences/shared_preferences.dart';

/// Same localStorage-token stopgap as both web apps' auth contexts (not the plan's target
/// secure-enclave / Keychain-backed storage) — harden all three together before real case
/// documents flow through any client.
class AuthStorage {
  static const _accessTokenKey = 'lp_access_token';
  static const _refreshTokenKey = 'lp_refresh_token';
  static const _userTypeKey = 'lp_user_type';

  Future<String?> readAccessToken() async => (await SharedPreferences.getInstance()).getString(_accessTokenKey);

  Future<String?> readUserType() async => (await SharedPreferences.getInstance()).getString(_userTypeKey);

  Future<void> save({required String accessToken, required String refreshToken, required String userType}) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_accessTokenKey, accessToken);
    await prefs.setString(_refreshTokenKey, refreshToken);
    await prefs.setString(_userTypeKey, userType);
  }

  Future<void> clear() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_accessTokenKey);
    await prefs.remove(_refreshTokenKey);
    await prefs.remove(_userTypeKey);
  }
}
