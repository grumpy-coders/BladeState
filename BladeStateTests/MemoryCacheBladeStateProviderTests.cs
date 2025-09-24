using GrumpyCoders.BladeState.Providers;
namespace GrumpyCoders.BladeStateTests;

[TestClass]
public sealed class MemoryCacheBladeStateProviderTests : TestBase
{
    private readonly MemoryCacheBladeStateProvider<AppState> Provider;
    private readonly AppState AppState = new();

    public MemoryCacheBladeStateProviderTests()
    {
        Provider = new(MemoryCache, Cryptography, Profile);
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
        await Provider.DisposeAsync().ConfigureAwait(false);
    }

}
