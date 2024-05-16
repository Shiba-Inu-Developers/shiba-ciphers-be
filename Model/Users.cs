using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace my_new_app.Model;

[Table("users")]
public class User
{
    [Key] // Az Id a primary key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("email")]
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Column("password")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }
    [Column("otp")]
    public string Otp { get; set; }

    [Column("verified")]
    public bool IsVerified { get; set; }

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("refresh_token_expiration")]
    public DateTime? RefreshTokenExpiryTime { get; set; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();
    }
}