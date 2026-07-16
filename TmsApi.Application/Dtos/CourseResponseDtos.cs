
namespace TmsApi.Application.Dtos;
public record CourseResponseDtos;

public record CourseResponseDto(
    int Id,
    string Code,
    string Title,
    int MaxCapacity,
    int EnrollmentCount
);