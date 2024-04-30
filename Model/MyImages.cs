using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace my_new_app.Model;

public class MyImages
{
    [Key] 
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int UserId { get; set; }

    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Hash { get; set; }
    public string? Source { get; set; }
    
    public string? Extension { get; set; }
    public string? Segmented { get; set; }
    public string? Encrypted { get; set; }
    public string? Decrypted { get; set; }
    public DateTime CreationDate { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }
}

public class MyImagesConfiguration : IEntityTypeConfiguration<MyImages>
{
    public void Configure(EntityTypeBuilder<MyImages> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.CreationDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId);

    }
}