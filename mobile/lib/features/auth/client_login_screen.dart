import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/auth/auth_provider.dart';
import '../../core/i18n/locale_provider.dart';

class ClientLoginScreen extends ConsumerStatefulWidget {
  const ClientLoginScreen({super.key});

  @override
  ConsumerState<ClientLoginScreen> createState() => _ClientLoginScreenState();
}

class _ClientLoginScreenState extends ConsumerState<ClientLoginScreen> {
  final _phoneController = TextEditingController();
  final _codeController = TextEditingController();
  bool _codeSent = false;
  bool _busy = false;
  String? _error;

  String get _phoneE164 => '+966${_phoneController.text.replaceFirst(RegExp(r'^0+'), '')}';

  Future<void> _sendCode() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(authApiProvider).requestClientOtp(_phoneE164);
      setState(() => _codeSent = true);
    } catch (_) {
      setState(() => _error = 'Could not send the code. Check the number.');
    } finally {
      setState(() => _busy = false);
    }
  }

  Future<void> _verifyCode() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final (accessToken, refreshToken) = await ref.read(authApiProvider).verifyClientOtp(_phoneE164, _codeController.text);
      await ref.read(authProvider.notifier).loginAsClient(accessToken, refreshToken);
      if (mounted) context.go('/');
    } catch (_) {
      setState(() => _error = 'That code is wrong or has expired.');
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
        appBar: AppBar(title: Text(isAr ? 'تسجيل الدخول' : 'Sign in')),
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (!_codeSent) ...[
                  Text(isAr ? 'رقم الجوال' : 'Mobile number'),
                  const SizedBox(height: 8),
                  TextField(
                    controller: _phoneController,
                    keyboardType: TextInputType.phone,
                    textDirection: TextDirection.ltr,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(prefixText: '+966 ', border: OutlineInputBorder(), hintText: '5XXXXXXXX'),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: _busy || _phoneController.text.length < 9 ? null : _sendCode,
                    child: Text(_busy ? (isAr ? 'جارٍ الإرسال…' : 'Sending…') : (isAr ? 'إرسال الرمز' : 'Send code')),
                  ),
                ] else ...[
                  Text(isAr ? 'أدخل الرمز المرسل إلى $_phoneE164' : 'Enter the code sent to $_phoneE164'),
                  const SizedBox(height: 8),
                  TextField(
                    controller: _codeController,
                    keyboardType: TextInputType.number,
                    textDirection: TextDirection.ltr,
                    maxLength: 6,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(border: OutlineInputBorder(), hintText: '000000'),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: _busy || _codeController.text.length != 6 ? null : _verifyCode,
                    child: Text(_busy ? (isAr ? 'جارٍ التحقق…' : 'Verifying…') : (isAr ? 'تأكيد الدخول' : 'Verify & continue')),
                  ),
                ],
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
