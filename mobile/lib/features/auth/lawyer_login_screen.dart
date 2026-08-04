import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/auth/auth_provider.dart';
import '../../core/i18n/locale_provider.dart';

class LawyerLoginScreen extends ConsumerStatefulWidget {
  const LawyerLoginScreen({super.key});

  @override
  ConsumerState<LawyerLoginScreen> createState() => _LawyerLoginScreenState();
}

class _LawyerLoginScreenState extends ConsumerState<LawyerLoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _busy = false;
  String? _error;

  Future<void> _login() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final (accessToken, refreshToken) = await ref.read(authApiProvider).lawyerLogin(_emailController.text, _passwordController.text);
      await ref.read(authProvider.notifier).loginAsLawyer(accessToken, refreshToken);
      if (mounted) context.go('/');
    } catch (_) {
      setState(() => _error = 'Incorrect email or password.');
    } finally {
      setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isAr = isArabic(ref.watch(localeProvider));

    return Directionality(
      textDirection: isAr ? TextDirection.rtl : TextDirection.ltr,
      child: Scaffold(
        appBar: AppBar(title: Text(isAr ? 'تسجيل دخول المحامي' : 'Lawyer sign in')),
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(isAr ? 'البريد الإلكتروني' : 'Email'),
                const SizedBox(height: 8),
                TextField(
                  controller: _emailController,
                  keyboardType: TextInputType.emailAddress,
                  textDirection: TextDirection.ltr,
                  onChanged: (_) => setState(() {}),
                  decoration: const InputDecoration(border: OutlineInputBorder()),
                ),
                const SizedBox(height: 16),
                Text(isAr ? 'كلمة المرور' : 'Password'),
                const SizedBox(height: 8),
                TextField(
                  controller: _passwordController,
                  obscureText: true,
                  textDirection: TextDirection.ltr,
                  onChanged: (_) => setState(() {}),
                  decoration: const InputDecoration(border: OutlineInputBorder()),
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _busy || _emailController.text.isEmpty || _passwordController.text.isEmpty ? null : _login,
                  child: Text(_busy ? (isAr ? 'جارٍ الدخول…' : 'Signing in…') : (isAr ? 'تسجيل الدخول' : 'Sign in')),
                ),
                if (_error != null) ...[
                  const SizedBox(height: 12),
                  Text(_error!, style: const TextStyle(color: Colors.red)),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
