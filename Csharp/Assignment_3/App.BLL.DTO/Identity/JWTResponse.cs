namespace App.BLL.DTO.Identity;

public class JWTResponse
{
    public string JWT { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}