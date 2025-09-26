using GrumpyCoders.BladeState.Providers;

namespace GrumpyCoders.BladeStateTests;

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
    [Ignore("Long-running timeout test, run manually only")]
    public async Task TimeoutTest()
    {
        await Provider.SaveStateAsync(AppState, CancellationToken);
        AppState state = await Provider.LoadStateAsync(CancellationToken);
        state.LastName = "Modified";
        await Task.Delay(Profile.InstanceTimeout + TimeSpan.FromSeconds(5), CancellationToken);
        state = await Provider.LoadStateAsync(CancellationToken);

        // State should be reset to default after timeout
        Assert.AreNotEqual("Modified", state.LastName);
    }

    [TestMethod]
    public async Task TestSaveAndRestore()
    {
        try
        {
            await Provider.SaveStateAsync(AppState, CancellationToken);
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
