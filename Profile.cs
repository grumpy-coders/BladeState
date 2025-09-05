using System;
namespace BladeState;

public class Profile
{
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string IpAddress { get; set; } = string.Empty;
	public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(12);
}