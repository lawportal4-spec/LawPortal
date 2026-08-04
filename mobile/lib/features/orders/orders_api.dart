import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/api/api_client.dart';

class OrderSummary {
  OrderSummary({
    required this.id,
    required this.number,
    required this.serviceNameAr,
    required this.serviceNameEn,
    required this.status,
    required this.subtotal,
  });

  factory OrderSummary.fromJson(Map<String, dynamic> json) => OrderSummary(
        id: json['id'] as String,
        number: json['number'] as String,
        serviceNameAr: json['serviceNameAr'] as String,
        serviceNameEn: json['serviceNameEn'] as String,
        status: json['status'] as String,
        subtotal: (json['subtotal'] as num?)?.toDouble(),
      );

  final String id;
  final String number;
  final String serviceNameAr;
  final String serviceNameEn;
  final String status;
  final double? subtotal;
}

class OrdersApi {
  OrdersApi(this._dio);
  final Dio _dio;

  Future<List<OrderSummary>> listMyRequests() async {
    final res = await _dio.get('/api/v1/client/requests', queryParameters: {'page': 1, 'pageSize': 50});
    final items = (res.data['items'] as List).cast<Map<String, dynamic>>();
    return items.map(OrderSummary.fromJson).toList();
  }
}

final ordersApiProvider = Provider((ref) => OrdersApi(ref.read(apiClientProvider)));
