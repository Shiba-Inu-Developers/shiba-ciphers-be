using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;

namespace my_new_app.DTOs;

public class UserDTO
{
    public int Id {get;set;}
    public string Email {get; set;}
    public string UserName {get; set;}

    public UserDTO()
    {
    }

}
