using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;

namespace my_new_app.DTOs;

public class CreateUserDTO
{
    public string Email { get; set; }
    public string Password { get; set; }
    public int Otp { get; set; }
    public bool IsVerified { get; set; }


    public CreateUserDTO()
    {
    }
}