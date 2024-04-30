using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;
using my_new_app.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using my_new_app.Service;
using System.Security.Cryptography;
using System.Text;


//TODO: TOTO LEN NA TESTOVANIE
using System.IO;
using System.Reflection;

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
    [Route("stepper-s0")]
    public async Task<IActionResult> stepper_s0(IFormFile image)
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
                        //steps:
                        //s1, s2t, s3t, s2k, s3k, s4, s5

                        //TODO: call service for classification
                        
                        Task<string> hashImageTask = ComputeImageHash(image);
                        string hashImage = await hashImageTask;
                        
                        // create record in database
                        var myImage = context.MyImages.FirstOrDefault(img => img.UserId == user.Id);
                        if (myImage == null)
                        {
                            myImage = new MyImages();
                            myImage.UserId = user.Id;
                            myImage.Type = "TextDocument";
                            myImage.Title = "DefaultTitle";
                            myImage.Content = "DefaultContent";
                            //myImage.Source = "DefaultSource";
                            myImage.Decrypted = "DefaultDecrypted";
                            myImage.Hash = hashImage;
                            context.MyImages.Add(myImage);
                        }
                        else
                        {
                            return BadRequest(new { message = "Image is already in the Database" });
                        }
                        
                        // upload image to cache (DS)
                        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                    
                        var filePath = Path.Combine(directoryPath, hashImage);
                        using (var stream = System.IO.File.Create(filePath))
                        {
                            await image.CopyToAsync(stream);
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
    
    [HttpPost]
    [Route("stepper-s1")]
    public async Task<IActionResult> stepper_s1(IFormFile image)
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
                    
                    // check datastore for folder with user email
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail);
                    if (!Directory.Exists(directoryPath))
                    {
                        return BadRequest(new { message = "No image in the Cache" });
                    }
                    
                    var subdirectories = Directory.GetDirectories(directoryPath);
                    if (subdirectories.Length == 0)
                    {
                        return BadRequest(new { message= "No subdirectories in the Cache" });
                    }
                    var firstSubdirectory = subdirectories[0];
                    
                    var finalPath = Path.Combine(directoryPath, firstSubdirectory);
                    var files = Directory.GetFiles(finalPath);
                    if (files.Length == 0)
                    {
                        return BadRequest(new { message= "No files in the subdirectory" });
                    }
                    
                    //TODO: send  to service for getting areas of text. Expect JSON
                    
                    
                    //dummy return, after service return JSON
                    return Ok(new { message = "Image areas to highlight" });

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
    
    
    [HttpPost]
    [Route("stepper-s2t")]
    public async Task<IActionResult> stepper_s2t(IFormFile image)
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
                    
                    // take areas from formdata
                    var form = Request.Form;
                    var areas = form["areas"];
                    
                    //TODO: send areas to service for text extraction. Expect zasifrovany text
                    var dummyText = "Zasifrovany text";
                    
                    
                    // check datastore for folder with user email
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail);
                    if (!Directory.Exists(directoryPath))
                    {
                        return BadRequest(new { message = "No image in the Cache" });
                    }
                    
                    var subdirectories = Directory.GetDirectories(directoryPath);
                    if (subdirectories.Length == 0)
                    {
                        return BadRequest(new { message= "No subdirectories in the Cache" });
                    }
                    var firstSubdirectory = subdirectories[0];
                    var hashForDB = firstSubdirectory;
                    
                    var myImage = context.MyImages.FirstOrDefault(img => img.UserId == user.Id && img.Hash == hashForDB);
                    if (myImage == null)
                    {
                        return BadRequest(new { message = "Image is not in the Database" });
                    }
                    myImage.Content = dummyText;
                    context.SaveChanges();
                    
                    //dummy return, after service return JSON
                    return Ok(new { message = dummyText });


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
    
    // ???
    // s3t
    // ???
    // ???
    // s3t
    // ???
    // ???
    // s3t
    // ???
    // ???
    // s3t
    // ???
    // ???
    // s3t
    // ???
    
    
    [HttpPost]
    [Route("stepper-s2k")]
    public async Task<IActionResult> stepper_s2k(IFormFile image)
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
                        
                        Task<string> hashImageTask = ComputeImageHash(image);
                        string hashImage = await hashImageTask;
                        
                        // create record in database
                        var myImage = context.MyImages.FirstOrDefault(img => img.UserId == user.Id);
                        if (myImage == null)
                        {
                            myImage = new MyImages();
                            myImage.UserId = user.Id;
                            myImage.Type = "KeyDocument";
                            myImage.Title = "DefaultTitle";
                            myImage.Content = "DefaultContent";
                            //myImage.Source = "DefaultSource";
                            myImage.Decrypted = "DefaultDecrypted";
                            myImage.Hash = hashImage;
                            context.MyImages.Add(myImage);
                        }
                        else
                        {
                            return BadRequest(new { message = "Image is already in the Database" });
                        }
                        
                        // upload image to cache (DS)
                        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }
                    
                        var filePath = Path.Combine(directoryPath, hashImage);
                        using (var stream = System.IO.File.Create(filePath))
                        {
                            await image.CopyToAsync(stream);
                        }
                        
                        //TODO: send to service for segmentation
                        
                        //dummy return, after service return JSON
                        return Ok(new { message = "JSON XD NEHEHE TU BUDE JSON HAHA HELP ME PLEASE THIS IS FUN IM DYING RETURN IS OK BUT IM NOT ACTUALLY OK IF YOU READ THIS IM 6 FEET UNDER IM LOST IN INFINITE VOID OF DARKNESS FIGHTING DEMONS OF ENDPOINTS" });
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
    
    
    
    //s3k
    
    [HttpPost]
    [Route("stepper-s3k")]
    public async Task<IActionResult> stepper_s3k(IFormFile image)
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
                    
                    // take areas from formdata
                    var form = Request.Form;
                    var areas = form["areas"];
                    
                    //TODO: send areas to service for text extraction. Expect zasifrovany JSON
                    var dummyKey = "Zasifrovany key";
                    
                    
                    // check datastore for folder with user email
                    var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail);
                    if (!Directory.Exists(directoryPath))
                    {
                        return BadRequest(new { message = "No image in the Cache" });
                    }
                    
                    var subdirectories = Directory.GetDirectories(directoryPath);
                    if (subdirectories.Length == 0)
                    {
                        return BadRequest(new { message= "No subdirectories in the Cache" });
                    }
                    var firstSubdirectory = subdirectories[0];
                    var hashForDB = firstSubdirectory;
                    
                    var myImage = context.MyImages.FirstOrDefault(img => img.UserId == user.Id && img.Hash == hashForDB);
                    if (myImage == null)
                    {
                        return BadRequest(new { message = "Image is not in the Database" });
                    }
                    myImage.Content = dummyKey;
                    context.SaveChanges();
                    
                    //dummy return, after service return JSON
                    return Ok(new { message = dummyKey });
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
    
    [HttpPost]
    [Route("stepper-s4")]
    public async Task<IActionResult> stepper_s4(IFormFile image1, IFormFile image2)
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
                    if (image1 != null && image1.Length > 0 && image2 != null && image2.Length > 0)
                    {
                        
                        Task<string> hashImageTask1 = ComputeImageHash(image1);
                        string hashImage1 = await hashImageTask1;
                        Task<string> hashImageTask2 = ComputeImageHash(image2);
                        string hashImage2 = await hashImageTask2;
                        
                        // find each image in Datastore
                        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "datastore", "Cache", userEmail);
                        if (!Directory.Exists(directoryPath))
                        {
                            return BadRequest(new { message = "No image in the Cache" });
                        }
                        
                        var subdirectories = Directory.GetDirectories(directoryPath);
                        if (subdirectories.Length == 0)
                        {
                            return BadRequest(new { message= "No subdirectories in the Cache" });
                        }
                        
                        var firstSubdirectory = subdirectories[0];
                        var hashForDB1 = firstSubdirectory;
                        
                        var myImage1 = context.MyImages.FirstOrDefault(img => img.UserId == user.Id && img.Hash == hashForDB1);
                        
                        if (myImage1 == null)
                        {
                            return BadRequest(new { message = "Image is not in the Database" });
                        }
                        
                        var secondSubdirectory = subdirectories[1];
                        var hashForDb2 = secondSubdirectory;
                        
                        var myImage2 = context.MyImages.FirstOrDefault(img => img.UserId == user.Id && img.Hash == hashForDb2);
                        
                        if (myImage2 == null)
                        {
                            return BadRequest(new { message = "Image is not in the Database" });
                        }
                        
                        // send to service for decryption
                        
                        
                        //dummy return, after service return text
                        return Ok(new { message = "Decrypted text" });
                        
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
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    public async Task<string> ComputeImageHash(IFormFile image)
    {
        using (var memoryStream = new MemoryStream())
        {
            await image.CopyToAsync(memoryStream);
            using (SHA256Managed sha = new SHA256Managed())
            {
                byte[] hash = sha.ComputeHash(memoryStream.ToArray());
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
    
    
    
    
    
    // function that will save the record about image being in the datastore into myImages database
    
    
    
    //TODO: endpoint -> final decipher, zober všetko z cache a pošli na service na dešifrovanie (text, kluc)
    
}