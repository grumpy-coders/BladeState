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
