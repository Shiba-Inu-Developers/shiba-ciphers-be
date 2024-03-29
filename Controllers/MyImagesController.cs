using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;
using my_new_app.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using my_new_app.Service;

namespace my_new_app.Controllers;

[ApiController]
[Route("[controller]")]
public class MyImagesController : ControllerBase
{
    private readonly ApplicationDbContext context;
    private readonly AuthService authService;

    public MyImagesController(ApplicationDbContext context)
    {
        this.context = context;
        authService = new AuthService(context);
    }
    
    
    //CREATE IMAGES
    [HttpPost]
    [Route("create-images")]
    public IActionResult CreateImage([FromBody] MyImageCreateDTO myImageCreate)
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                if (myImageCreate == null)
                {
                    return BadRequest(new { message = "Invalid data" });
                }
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userEmail = authService.GetEmailFromToken(token);
                var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    var image = new MyImages();
                    image.UserId = user.Id;
                    image.Type = myImageCreate.Type;
                    image.Title = myImageCreate.Title;
                    image.Content = myImageCreate.Content;
                    image.Source = myImageCreate.Source;
                    image.Decrypted = myImageCreate.Decrypted;
                    context.MyImages.Add(image);
                    context.SaveChanges();
                    return Ok(new { message = "Image was created!" });
                }
                return BadRequest(new { message = "No authorization token provided" });
                
            }
            return BadRequest(new { message = "No authorization token provided" }); 
        }catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
    
    [HttpGet]
    //History
    [Route("history-images")]
    public IActionResult GetImage()
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
               
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userEmail = authService.GetEmailFromToken(token);
                var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    ICollection<MyImages> images = context.MyImages.Where(img => img.UserId == user.Id).ToList();
                    var imageDTO = images.Select(myimage => new ImageHistoryDTO()
                    {
                        Type = myimage.Type,
                        Title = myimage.Title,
                        Content = myimage.Content,
                        Decrypted = myimage.Decrypted,
                        CreationDate = myimage.CreationDate
                    }).ToList();
                    return Ok(imageDTO);
                }
                return BadRequest(new { message = "No authorization token provided" });
                
            }
            return BadRequest(new { message = "No authorization token provided" });
        }catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
    
}