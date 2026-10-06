using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using Api.Models.Auth;

using Api.Models.Products;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
namespace Api.Controllers;
[EnableRateLimiting("fixed")]
[ApiController]
[Route("api/[controller]")]

public class ProductsController : ControllerBase
{
  private readonly ProductService _productService;
  public ProductsController(ProductService productService)
  {
    _productService = productService;
  }

  [HttpPost]
  public async Task<IActionResult> Create(CreateProductRequest request)
  {
    var product = await _productService.CreateAsync(request);
    return Created($"/api/products/{product.Id}", product);
  }


  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var products = await _productService.GetAllAsync();

    return Ok(products);
  }



  [Authorize]
  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(int id)
  {
    var deleted = await _productService.DeleteAsync(id);

    if (!deleted)
      return NotFound();

    return NoContent();
  }


 [Authorize]
  [HttpPut("{id}")]
  public async Task<IActionResult> Update(
    int id,
    UpdateProductRequest request)
  {
    var product = await _productService.UpdateAsync(id, request);

    if (product is null)
      return NotFound();

    return Ok(product);
  }

[HttpGet("search")]
public async Task<IActionResult> Search(
    [FromQuery] string title,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    if (string.IsNullOrWhiteSpace(title))
        return BadRequest("Title is required.");

    if (page < 1)
        return BadRequest("Page must be greater than 0.");

    if (pageSize < 1 || pageSize > 100)
        return BadRequest("PageSize must be between 1 and 100.");

    var products = await _productService.SearchAsync(
        title,
        page,
        pageSize);

    return Ok(products);
}
}




[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result)
            return Conflict("Username already exists.");

        return Ok("User registered successfully.");
    }


[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    var token = await _authService.LoginAsync(request);

    if (token is null)
        return Unauthorized("Username or password is incorrect.");

    return Ok(new
    {
        token
    });
}
}