export interface ApiEnvelope<T> {
  isSuccess: boolean;
  code: string;
  message: string;
  data: T | null;
}

export interface TokensDto {
  token: string;
  refreshToken: string;
}

export interface LoginResponseDto {
  tokens: TokensDto;
  isNeededChangePassword: boolean;
}

export interface ProfileDto {
  id: number;
  name: string;
  email: string;
  role: string | null;
  isNeededChangePassword: boolean;
  createdAt: string;
}

export interface RegistrationStatusDto {
  enabled: boolean;
}
