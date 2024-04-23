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
    
    
    //Get concrete dokument by Date for concrete user 
    [HttpGet]
    [Route("date-image")]
    public IActionResult GetImageByDate()
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
                    var images = context.MyImages
                        .Where(img => img.UserId == user.Id)
                        .ToList();

                    if (images.Any())
                    {
                        var closestImage = images
                            .OrderBy(img => Math.Abs((img.CreationDate - DateTime.Now).Ticks))
                            .First();

                        var imageDTO = new ImageHistoryDTO()
                        {
                            Type = closestImage.Type,
                            Title = closestImage.Title,
                            Content = closestImage.Content,
                            Decrypted = closestImage.Decrypted,
                            CreationDate = closestImage.CreationDate
                        };

                        return Ok(imageDTO);
                    }
                    else
                    {
                        return BadRequest(new { message = "No images found for the user" });
                    }
                }
                else
                {
                    return BadRequest(new { message = "User not found" });
                }
            }
            else
            {
                return BadRequest(new { message = "No authorization token provided" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
    
    
    //PUT for update decrypted text only, by Id image
    [HttpPut]
    [Route("decrypt-images")]
    public IActionResult DecryptImage([FromBody] DecryptImageDTO decryptImageDto)
    {
        try
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                if (decryptImageDto == null || decryptImageDto.Id==null)
                {
                    return BadRequest(new { message = "Invalid data" });
                }
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userEmail = authService.GetEmailFromToken(token);
                var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);
                if (user != null)
                {
                    var image  = context.MyImages.FirstOrDefault(u => u.UserId == user.Id);
                    if (image.Id != decryptImageDto.Id)
                    {
                        return BadRequest(new { message = "Image is not correct" });
                    }
                    image.Decrypted = decryptImageDto.Decrypted;
                    context.SaveChanges();
                    return Ok(new { message = "Decrypted text was updated!" });
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
    
    //PUT update stepper?
    //Get Image by ID?
    //GET ...?
    
    

    [HttpPost]
    [Route("save-image-ds")]
    public async Task<IActionResult> SaveImage(IFormFile image)
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
                    if (image != null && image.Length > 0)
                    {
                        
                        string imageType;
                        HttpContext.Request.Cookies.TryGetValue("imageType", out imageType);
                        

                        if (imageType == "Text")
                            {
                                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail, "TextFolder");
                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                            
                                var filePath = Path.Combine(directoryPath, "TextNameHere.png");
                                using (var stream = System.IO.File.Create(filePath))
                                {
                                    await image.CopyToAsync(stream);
                                }
                            }
                        else if (imageType == "Key")
                            {
                                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail, "KeyFolder");
                                if (!Directory.Exists(directoryPath))
                                {
                                    Directory.CreateDirectory(directoryPath);
                                }
                            
                                var filePath = Path.Combine(directoryPath, "KeyNameHere.png");
                                using (var stream = System.IO.File.Create(filePath))
                                {
                                    await image.CopyToAsync(stream);
                                }
                            }
                        else
                        {
                            return BadRequest(new { message = "No image type provided" });
                        }
                        
                        return Ok(new { message = "Image was saved!" });
                    }
                    else
                    {
                        return BadRequest(new { message = "No image provided" });
                    }
                }
                else
                {
                    return BadRequest(new { message = "No authorization token provided" });
                }
            }
            else
            {
                return BadRequest(new { message = "No authorization token provided" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
    
}