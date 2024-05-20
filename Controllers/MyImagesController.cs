using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;
using my_new_app.DTOs;
using my_new_app.Service;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;


namespace my_new_app.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class MyImagesController : ControllerBase
{
    private readonly ApplicationDbContext context;
    private readonly AuthService authService;
    private readonly HttpClient _httpClient;

    public MyImagesController(ApplicationDbContext context, HttpClientService httpClientService)
    {
        this.context = context;
        authService = new AuthService(context);
        _httpClient = httpClientService.CreateClient();
    }

    [HttpGet]
    [Route("history-images")]
    public IActionResult GetImage()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        return Ok(context.MyImages.Where(i => i.UserId == user.Id).Select(i => new ImageHistoryDTO()
        {
            Type = i.Type,
            Title = i.Title,
            CreationDate = i.CreationDate
        }).ToList());
    }

    [HttpGet]
    [Route("text-records")]
    public IActionResult GetImageTexts()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        return Ok(context.MyImages.Where(i => i.UserId == user.Id && i.Type == "text").Select(i => new ImageHistoryDTO()
        {
            Type = i.Type,
            Title = i.Title,
            CreationDate = i.CreationDate
        }).ToList());
    }

    [HttpGet]
    [Route("key-records")]
    public IActionResult GetImageKeys()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        return Ok(context.MyImages.Where(i => i.UserId == user.Id && i.Type == "key").Select(i => new ImageHistoryDTO()
        {
            Type = i.Type,
            Title = i.Title,
            CreationDate = i.CreationDate
        }).ToList());
    }

    [HttpGet]
    [Route("date-image")]
    public IActionResult GetImageByDate()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var now = DateTime.Now;
        var image = context.MyImages.Where(i => i.UserId == user.Id).OrderBy(i => Math.Abs((i.CreationDate - now).Ticks)).SingleOrDefault();

        if (image == null)
        {
            return BadRequest("No image found");
        }

        return Ok(new ImageHistoryDTO { Type = image.Type, Title = image.Title, CreationDate = image.CreationDate });
    }

    [HttpPut]
    [Route("stepper-s0")]
    public async Task<IActionResult> stepper_s0(IFormFile image, [FromForm] string imageTitle)
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        if (image == null || image.Length <= 0)
        {
            return BadRequest("No image provided");
        }

        var ext_idx = image.FileName.LastIndexOf('.');

        if (ext_idx == -1)
        {
            return BadRequest("Input file needs extension in its name");
        }

        var ext = image.FileName.Substring(ext_idx + 1);
        string? hash = null;
        using (var sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(image.OpenReadStream());
            hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        var myImage = context.MyImages.Where(img => img.UserId == user.Id && img.Hash == hash).FirstOrDefault();

        var filePath = Path.Combine("/mnt", "datastore", hash);
        using (var stream = System.IO.File.Create(filePath))
        {
            await image.CopyToAsync(stream);
        }

        if (myImage != null)
        {
            myImage.Extension = ext;
            myImage.Title = imageTitle;
        }
        else
        {
            myImage = new MyImages
            {
                UserId = user.Id,
                Extension = ext,
                Hash = hash,
                Title = imageTitle
            };
            context.MyImages.Add(myImage);
        }

        context.SaveChanges();

        var multipart = new MultipartFormDataContent
                        {
                            { new StreamContent(image.OpenReadStream()), "image" , image.FileName}
                        };

        var response = await _httpClient.PostAsync("http://sv:6000/classify/" + hash, multipart);

        return Ok(await response.Content.ReadAsStringAsync());
    }

    private enum ImageType
    {
        Key,
        Text
    }

    [HttpPost]
    [Route("stepper-s1k/{hash}")]
    public async Task<IActionResult> stepper_s1k(string hash)
    {
        return await segment(hash, ImageType.Key);
    }

    [HttpPost]
    [Route("stepper-s1t/{hash}")]
    public async Task<IActionResult> stepper_s1t(string hash)
    {
        return await segment(hash, ImageType.Text);
    }

    private async Task<IActionResult> segment(string hash, ImageType type)
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var myImage = context.MyImages.FirstOrDefault(img => img.Hash == hash);

        if (myImage == null)
        {
            return BadRequest("Image not found");
        }

        if (myImage.UserId != user.Id)
        {
            return Unauthorized("User does not own the image");
        }

        switch (type)
        {
            case ImageType.Key:
                myImage.Type = "key";
                break;
            case ImageType.Text:
                myImage.Type = "text";
                break;
        }

        context.SaveChanges();

        var response = type switch
        {
            ImageType.Key => await _httpClient.PostAsync("http://sv:6000/segment_key/" + hash, null),
            ImageType.Text => await _httpClient.PostAsync("http://sv:6000/segment_text/" + hash, null),
            _ => throw new NotImplementedException()
        };

        return Ok(await response.Content.ReadAsStringAsync());
    }

    public class Area
    {
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string? type { get; set; }
    }

    public class ExtractRequest
    {
        [Required]
        public List<Area>? areas { get; set; }
    }

    public class ExtractResponse
    {
        [Required]
        public List<JsonElement>? contents { get; set; }
    }

    [HttpPost]
    [Route("stepper-s2k/{hash}")]
    public async Task<IActionResult> stepper_s2k(string hash, ExtractRequest areas)
    {
        return await stepper_s2(hash, areas, ImageType.Key);
    }

    [HttpPost]
    [Route("stepper-s2t/{hash}")]
    public async Task<IActionResult> stepper_s2t(string hash, ExtractRequest areas)
    {
        return await stepper_s2(hash, areas, ImageType.Text);
    }
    private async Task<IActionResult> stepper_s2(string hash, ExtractRequest areas, ImageType type)
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var myImage = context.MyImages.FirstOrDefault(img => img.Hash == hash);

        if (myImage == null)
        {
            return BadRequest("Image not found");
        }

        if (myImage.UserId != user.Id)
        {
            return Unauthorized("User does not own the image");
        }

        var requestUri = type switch
        {
            ImageType.Key => new Uri("http://sv:6000/extract_key/" + hash),
            ImageType.Text => new Uri("http://sv:6000/extract_text/" + hash),
            _ => throw new NotImplementedException()
        };

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = requestUri,
            Content = new StringContent(JsonSerializer.Serialize(areas), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();
        var contents = JsonSerializer.Deserialize<ExtractResponse>(responseJson);

        for (int i = 0; i < contents.contents.Count; i++)
        {
            string? content = JsonSerializer.Serialize(contents.contents[i]);
            var data = JsonSerializer.Serialize(areas.areas[i]);
            var existing = context.Segments.Where(s => s.ImageId == myImage.Id && s.data == data).FirstOrDefault();

            if (existing != null)
            {
                existing.content = content;
            }
            else
            {
                context.Segments.Add(new Segment { ImageId = myImage.Id, data = data, content = content });
            }
        }

        myImage.Content = responseJson;
        context.SaveChanges();

        return Ok(responseJson);
    }

    public class SaveRequest
    {
        [Required]
        public string? title { get; set; }
        [Required]
        public string? description { get; set; }
        [Required]
        public JsonElement? content { get; set; }
    }

    [HttpPost]
    [Route("save/{hash}")]
    public async Task<IActionResult> Save(string hash, SaveRequest data)
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var myImage = context.MyImages.FirstOrDefault(img => img.Hash == hash);

        if (myImage == null)
        {
            return BadRequest("Image not found");
        }

        if (myImage.UserId != user.Id)
        {
            return Unauthorized("User does not own the image");
        }

        myImage.Title = data.title;
        myImage.Description = data.description;
        myImage.Content = JsonSerializer.Serialize(data.content);
        context.SaveChanges();

        return Ok("");
    }

    public class DecryptRequest
    {
        [Required]
        public int? key { get; set; }
        [Required]
        public int? text { get; set; }
    }

    [HttpPost]
    [Route("decrypt")]
    public async Task<IActionResult> Decrypt(DecryptRequest decryptRequest)
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var key = context.MyImages.Where(i => i.Id == decryptRequest.key).SingleOrDefault();

        if (key == null)
        {
            return BadRequest("Image not found");
        }

        if (key.UserId != user.Id)
        {
            return Unauthorized("User does not own the key document");
        }

        if (key.Content == null)
        {
            return BadRequest("The key document has not been properly saved");
        }

        var text = context.MyImages.Where(i => i.Id == decryptRequest.text).SingleOrDefault();

        if (text == null)
        {
            return BadRequest("Image not found");
        }

        if (text.UserId != user.Id)
        {
            return Unauthorized("User does not own the text document");
        }

        if (text.Content == null)
        {
            return BadRequest("The text document has not been properly saved");
        }

        var response = await _httpClient.PostAsync("http://sv:6000/decrypt", new StringContent(JsonSerializer.Serialize(new { key = JsonDocument.Parse(key.Content).RootElement, text = text.Content }), Encoding.UTF8, "application/json"));
        var responseString = await response.Content.ReadAsStringAsync();
        text.DecryptedContent = responseString;

        context.SaveChanges();

        return Ok(responseString);
    }


    [HttpGet]
    [Route("all-images")]
    public IActionResult GetAllImages()
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var images = context.MyImages.Where(i => i.UserId == user.Id)
            .Select(i => new
            {
                Id = i.Id,
                UserId = i.UserId,
                Type = i.Type,
                Title = i.Title,
                Description = i.Description,
                Hash = i.Hash,
                Extension = i.Extension,
                Content = i.Content,
                DecryptedContent = i.DecryptedContent,
                CreationDate = i.CreationDate
            })
            .ToList();

        return Ok(images);
    }


    [HttpGet]
    [Route("get-image-ds/{hash}")]
    public IActionResult GetImageDs(string hash)
    {
        var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var user = context.MyUsers.FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            return Unauthorized("Invalid token");
        }

        var myImage = context.MyImages.FirstOrDefault(img => img.Hash == hash);

        if (myImage == null)
        {
            return BadRequest("Image not found");
        }

        if (myImage.UserId != user.Id)
        {
            return Unauthorized("User does not own the image");
        }

        var filePath = Path.Combine("/mnt", "datastore", hash);
        var contentType = "image/" + myImage.Extension;

        return PhysicalFile(filePath, contentType);
    }

}