namespace LawPortal.Application.Calls.Dtos;

public record CallTokenDto(string LiveKitUrl, string Token, string RoomName, int AllowedDurationSeconds, int? ElapsedSeconds);
