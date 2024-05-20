using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace my_new_app.Model;

[Table("images")]
public class MyImages
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("type")]
    public string? Type { get; set; }
    [Column("title")]
    public string? Title { get; set; }
    [Column("description")]
    public string? Description { get; set; }
    [Column("public")]
    public bool? Public { get; set; }
    [Column("hash")]
    public string? Hash { get; set; }
    [Column("ext")]
    public string? Extension { get; set; }
    [Column("content")]
    public string? Content { get; set; }
    [Column("decrypted_content")]
    public string? DecryptedContent { get; set; }
    [Column("creation_date")]
    public DateTime CreationDate { get; set; }

    [ForeignKey(nameof(UserId))]
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

        builder.Property(p => p.Public).HasDefaultValue(false);

        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId);

    }
}