namespace StatusPageSharp.Domain.Entities;

public class ServiceGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public List<Service> Services { get; set; } = [];
}
