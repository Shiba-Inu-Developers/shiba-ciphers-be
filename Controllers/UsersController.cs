using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;
using my_new_app.DTOs; 
namespace my_new_app.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext context;

    public UsersController(ApplicationDbContext context)
    {
        this.context = context;
    }

    [HttpGet]
    [Route("Users")]
    public IEnumerable<User> GetUsers()
    {
        var users = context.MyUsers.ToList();
        return users;
    }
    
    [HttpGet]
    [Route("User/{id}")]
     public IActionResult GetUserById(int id){
        //var user = context.MyUsers.FirstOrDefault(u => u.Id == id);
        var users = context.MyUsers.ToList();
        User user = null;
        foreach (var u in users)
        {
            if(u.Id == id){
                user = u;
            }
        }
        if (user == null)
        {
            return NotFound(); 
        }
        var userDto = new UserDTO
        {
        Id = user.Id,
        Email = user.Email,
        UserName = user.Email
        };

        return Ok(userDto);
    }
    [HttpPost]
    [Route("User")]
    public IActionResult CreateUser([FromBody] CreateUserDTO createUserDTO)
    {
        try{
        if (createUserDTO == null)
        {
            return BadRequest("Invalid data");
        }
        if (context.MyUsers.Any(u => u.Email == createUserDTO.Email))
        {
            return Conflict(new { error = "A user with this email already exists." });
            //return BadRequest(new { error = "Invalid request. Please check the data provided." }); //?

        }

        var newUser = new User
        {
            Email = createUserDTO.Email,
            Password = createUserDTO.Password,
        };

        context.MyUsers.Add(newUser);
        context.SaveChanges();

        var createdUserDTO = new UserDTO
        {
            Id = newUser.Id,
            Email = newUser.Email,
            UserName = newUser.Email
        };

        return CreatedAtAction(nameof(GetUserById), new { id = createdUserDTO.Id }, createdUserDTO);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return StatusCode(500, new { error = "Internal Server Error" });
    }

}
}
