namespace my_new_app.DTOs;

public class ImageHistoryDTO
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Decrypted { get; set; }
    public DateTime CreationDate { get; set; }
    public bool? Public { get; set; }
}