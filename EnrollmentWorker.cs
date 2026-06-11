using Microsoft.Extensions.DependencyInjection;



public class EnrollmentWorker
{
   

    private readonly IServiceScopeFactory _scopeFactory;

    // Constructor injecting the scoped service into this singleton
    
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

