namespace App.BLL.DTO.Admin;

public class AdminProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltTextEn { get; set; } = string.Empty;
    public string AltTextEt { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
