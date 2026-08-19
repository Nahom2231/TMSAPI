namespace TmsApi.Domain.Entities;

public class RefreshToken
{
    public int Id {get; set; }
    public string Token { get; set; }= string.Empty;
    public string UserId {get; set;}= string.Empty;

    public DateTime ExpiredAt { get; set; } 

    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set;}
}