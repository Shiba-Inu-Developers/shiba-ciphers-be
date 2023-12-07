using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace my_new_app.Model;

public class User
{
    [Key] // Az Id a primary key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] [EmailAddress] public string Email { get; set; }

    [DataType(DataType.Password)] public string Password { get; set; }

    public string Otp { get; set; }

    public bool IsVerified { get; set; }

    public string RefreshToken { get; set; }

    public DateTime RefreshTokenExpiryTime { get; set; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();
    }
}