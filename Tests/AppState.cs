namespace GrumpyCoders.BladeStateTests;

public class AppState
{
	public string FirstName { get; set; } = "Blade";
	public string LastName { get; set; } = "State";
	public int Age { get; set; } = 35;
	public List<string> Tags { get; set; } = ["state", "management", "csharp", "dotnet"];
	public Dictionary<string, string> Metadata { get; set; } = new()
	{
		{ "env", "test" },
		{ "version", "1.0.0" },
		{ "author", "BladeState" }
	};
	public NestedState Nested { get; set; } = new();
}