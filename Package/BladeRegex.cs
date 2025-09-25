using System.Text.RegularExpressions;
namespace GrumpyCoders.BladeState;

public static partial class BladeRegex
{
	[GeneratedRegex(@"^[A-Za-z0-9_]+$")]
	public static partial Regex LettersAndNumbers();
}
