import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/api/api_client.dart';

class IncomingRequest {
  IncomingRequest({
    required this.id,
    required this.number,
    required this.clientName,
    required this.serviceNameAr,
    required this.serviceNameEn,
    required this.status,
    required this.subtotal,
  });

  factory IncomingRequest.fromJson(Map<String, dynamic> json) => IncomingRequest(
        id: json['id'] as String,
        number: json['number'] as String,
        clientName: json['clientName'] as String,
        serviceNameAr: json['serviceNameAr'] as String,
        serviceNameEn: json['serviceNameEn'] as String,
        status: json['status'] as String,
        subtotal: (json['subtotal'] as num?)?.toDouble(),
      );

  final String id;
  final String number;
  final String clientName;
  final String serviceNameAr;
  final String serviceNameEn;
  final String status;
  final double? subtotal;
}

class LawyerRequestsApi {
  LawyerRequestsApi(this._dio);
  final Dio _dio;

  Future<List<IncomingRequest>> listIncoming() async {
    final res = await _dio.get('/api/v1/lawyer/requests', queryParameters: {'page': 1, 'pageSize': 50});
    final items = (res.data['items'] as List).cast<Map<String, dynamic>>();
    return items.map(IncomingRequest.fromJson).toList();
  }

  Future<void> accept(String id) => _dio.post('/api/v1/lawyer/requests/$id/accept');

  Future<void> complete(String id) => _dio.post('/api/v1/lawyer/requests/$id/complete');
}

final lawyerRequestsApiProvider = Provider((ref) => LawyerRequestsApi(ref.read(apiClientProvider)));
