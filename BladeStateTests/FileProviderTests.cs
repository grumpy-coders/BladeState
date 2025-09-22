using BladeState.Providers;
namespace BladeStateTests;

[TestClass]
public sealed class FileProviderTests : TestBase
{
    [TestMethod]
    public async Task TestSaveAndRestore()
    {
        try
        {
            AppState appState = new();
            FileSystemBladeStateProvider<AppState> provider = new(Cryptography, Profile);
            await provider.SaveStateAsync(appState, CancellationToken);
            if (!File.Exists(provider.GetFileFilePath()))
            {
                Assert.Fail($"State file {provider.GetFileFilePath()} was not created.");
            }
            appState = await provider.LoadStateAsync(CancellationToken);
            Assert.IsTrue(CheckAppState(appState));
        }
        catch (Exception exception)
        {
            Assert.Fail(exception.Message);
        }
    }
}
