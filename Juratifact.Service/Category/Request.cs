namespace Juratifact.Service.Category;

public class Request
{
    public class CreateCategoryRequest
    {
        public required string Name { get; set; }
        public Guid? ParentId { get; set; }
    }
    
    public class UpdateCategoryRequest
    {
        public required string Name { get; set; }
    }
}