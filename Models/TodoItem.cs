using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TodoPlus.Models
{
    public class TodoItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [Display(Name = "Owner User ID")]
        public string? UserId { get; set; }

        [BsonIgnoreIfNull]
        [Display(Name = "Owner Username")]
        public string? OwnerUsername { get; set; }

        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(120, ErrorMessage = "Title cannot exceed 120 characters.")]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DueDate { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Priority Priority { get; set; } = Priority.Medium;

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string? Category { get; set; } = "General";

        [Display(Name = "Created Date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Completed Date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? CompletedAt { get; set; }
    }
}
