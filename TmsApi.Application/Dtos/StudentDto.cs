using System.Text.Json.Serialization;

namespace TmsApi.Application.DTOs;

public record StudentDto(int Id, string FullName, string Email)
{
    [JsonIgnore]
    public string? InternalNotes {get; init;}
}