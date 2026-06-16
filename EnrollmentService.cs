namespace TmsApi;

public interface IEnrollmentService
{
    Task<IEnumerable<object>> GetAllAsync();
    Task<object?> GetByIdAsync(string id);
    Task<dynamic> EnrollAsync(string studentId, string courseCode);

    Task<bool> DeleteAsync(string id);

}

public class EnrollmentService : IEnrollmentService
{
    public async Task<IEnumerable<object>> GetAllAsync()
    {
        return new List<object>();
    }

    public async Task<object?> GetByIdAsync(string id)
    {
        return null;
    }
    public async Task<dynamic> EnrollAsync(string studentId, string courseCode)
    {
        // Dummy implementation
      var newRecord = new
      {
        Id = Guid.NewGuid().ToString(),
        StudentId = studentId,
        CourseCode = courseCode,
        
     
      };
      return await Task.FromResult(newRecord);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await Task.Delay(10);
        return true;
    }
}