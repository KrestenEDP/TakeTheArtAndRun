namespace TakeTheArtAndRunAPI.Models;

public class Artist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = String.Empty;
    public string Bio { get; set; } = String.Empty;
}
