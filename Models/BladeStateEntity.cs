namespace BladeState.Models;

public class BladeStateEntity
{
    public string InstanceId { get; set; } = string.Empty; // BladeState-Profile.Id
    public string StateData { get; set; } = string.Empty; // encrypted or plain JSON
}
