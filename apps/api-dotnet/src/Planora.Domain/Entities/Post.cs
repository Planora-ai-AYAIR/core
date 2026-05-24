namespace Planora.Domain.Entities;

public sealed class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public string Body { get; set; }

}
