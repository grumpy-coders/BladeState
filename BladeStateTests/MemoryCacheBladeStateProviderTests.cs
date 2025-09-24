using GrumpyCoders.BladeState.Providers;
namespace GrumpyCoders.BladeStateTests;

[TestClass]
public sealed class MemoryCacheBladeStateProviderTests : TestBase
{
    private readonly FileSystemBladeStateProvider<AppState> Provider;
    private readonly AppState AppState = new();

    public MemoryCacheBladeStateProviderTests()
    {
        Provider = new(Cryptography, Profile);
    }

    [TestMethod]
    public async Task TestSaveAndRestore()
    {
        try
        {
            await Provider.SaveStateAsync(AppState, CancellationToken);
            if (!File.Exists(Provider.GetFilePath()))
            {
                Assert.Fail($"State file {Provider.GetFilePath()} was not created.");
            }
            Assert.IsTrue(CheckAppState(await Provider.LoadStateAsync(CancellationToken)));
        }
        catch (Exception exception)
        {
            Assert.Fail(exception.Message);
        }
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        string filePath = Provider.GetFilePath();
        await Provider.DisposeAsync().ConfigureAwait(false);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Assert.Fail($"State file {filePath} was not deleted on dispose.");
        }
    }

}
