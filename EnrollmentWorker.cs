

public class EnrollmentWorker
{
    private readonly IEnrollmentService _enrollmentService;

    private readonly IServiceScopeFactory _scopeFactory;

    // Constructor injecting the scoped service into this singleton
    public EnrollmentWorker(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }
    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
         using (var scope = _scopeFactory.CreateScope())
    {
        var enrollmentService = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();
    }
    }
}

