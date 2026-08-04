import { api } from "./api";

export interface AuthResultDto {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  userId: string;
  userType: string;
}

export async function lawyerLogin(email: string, password: string): Promise<AuthResultDto> {
  const { data } = await api.post<AuthResultDto>("/api/v1/auth/lawyer/login", {
    email,
    password,
    recaptchaToken: "dev-placeholder",
  });
  return data;
}

export interface RegisterLawyerBody {
  fullName: string;
  email: string;
  password: string;
  regionId: number | null;
  cityId: number | null;
  licenseNumber: string;
  issueDate: string;
  expiryDate: string;
}

export async function registerLawyer(body: RegisterLawyerBody): Promise<AuthResultDto> {
  const { data } = await api.post<AuthResultDto>("/api/v1/auth/lawyer/register", {
    ...body,
    recaptchaToken: "dev-placeholder",
  });
  return data;
}

export interface CityDto {
  id: number;
  nameAr: string;
  nameEn: string;
}

export interface RegionDto {
  id: number;
  nameAr: string;
  nameEn: string;
  cities: CityDto[];
}

export async function getRegions(): Promise<RegionDto[]> {
  const { data } = await api.get<RegionDto[]>("/api/v1/catalog/regions");
  return data;
}
