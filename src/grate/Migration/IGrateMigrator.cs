namespace grate.Migration;

public interface IGrateMigrator : IAsyncDisposable
{
    Task Migrate();
}
