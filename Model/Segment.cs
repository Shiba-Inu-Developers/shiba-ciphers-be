using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

[Table("segments")]
[PrimaryKey(nameof(ImageId), nameof(data))]
[EntityTypeConfiguration(typeof(SegmentConfiguration))]
public class Segment
{
    [Column("image_id")]
    public int ImageId { get; set; }
    [Column("data")]
    public string data { get; set; }
    [Column("content")]
    public string content { get; set; }
}

public class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {

    }
}