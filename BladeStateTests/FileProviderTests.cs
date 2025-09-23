using BladeState.Providers;
namespace BladeStateTests;

[TestClass]
public sealed class FileProviderTests : TestBase
{
    private readonly FileSystemBladeStateProvider<AppState> Provider;
    private readonly AppState AppState = new();

    public FileProviderTests()
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
        // await Provider.DisposeAsync().ConfigureAwait(false);
    }

}
