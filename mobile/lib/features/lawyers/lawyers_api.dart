import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/api/api_client.dart';

class LawyerCard {
  LawyerCard({
    required this.id,
    required this.fullName,
    required this.cityNameAr,
    required this.cityNameEn,
    required this.avgRating,
    required this.writtenPrice,
    required this.isVerified,
  });

  factory LawyerCard.fromJson(Map<String, dynamic> json) => LawyerCard(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        cityNameAr: json['cityNameAr'] as String?,
        cityNameEn: json['cityNameEn'] as String?,
        avgRating: (json['avgRating'] as num?)?.toDouble(),
        writtenPrice: (json['writtenPrice'] as num).toDouble(),
        isVerified: json['isVerified'] as bool,
      );

  final String id;
  final String fullName;
  final String? cityNameAr;
  final String? cityNameEn;
  final double? avgRating;
  final double writtenPrice;
  final bool isVerified;
}

class LawyersApi {
  LawyersApi(this._dio);
  final Dio _dio;

  Future<List<LawyerCard>> search({String? q, int page = 1, int pageSize = 20}) async {
    final res = await _dio.get('/api/v1/lawyers', queryParameters: {
      if (q != null && q.isNotEmpty) 'q': q,
      'page': page,
      'pageSize': pageSize,
    });
    final items = (res.data['items'] as List).cast<Map<String, dynamic>>();
    return items.map(LawyerCard.fromJson).toList();
  }
}

final lawyersApiProvider = Provider((ref) => LawyersApi(ref.read(apiClientProvider)));
