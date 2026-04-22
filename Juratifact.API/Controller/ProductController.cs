using Juratifact.Service.Models;
using Juratifact.Service.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetProducts(int pageSize = 10, int pageIndex = 1)
    {
        var product = await _productService.GetAll(pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(product,HttpContext.TraceIdentifier));
    }
    
    [HttpGet("Title")]
    public async Task<IActionResult> GetByTitle(string? searchTerm,int pageSize = 10, int pageIndex = 1)
    {
        var product = await _productService.GetByTitle(searchTerm,pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(product,HttpContext.TraceIdentifier));
    }
    
    [HttpGet("Condition")]
    public async Task<IActionResult> GetByCondition(string? searchTerm,int pageSize = 10, int pageIndex = 1)
    {
        var product = await _productService.GetByCondition(searchTerm,pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(product,HttpContext.TraceIdentifier));
    }
}