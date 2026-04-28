using Juratifact.API.Extensions;
using Juratifact.Service.Models;
using Juratifact.Service.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;


[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> GetByCondition(string? searchTerm, int pageSize = 10, int pageIndex = 1)
    {
        var product = await _productService.GetByCondition(searchTerm, pageSize, pageIndex);
        return Ok(ApiResponseFactory.SuccessResponse(product, HttpContext.TraceIdentifier));

    }
    
    [HttpGet("{productId}/comments")]
    public async Task<IActionResult> GetCommentsByProductId(Guid productId)
    {
        var comments = await _productService.GetCommentsByProductId(productId);
        return Ok(ApiResponseFactory.SuccessResponse(comments, HttpContext.TraceIdentifier));
    }
    

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("Post")]
    public async Task<IActionResult> CreateProduct([FromForm] Request.CreateProductRequest request)
    {
        var result = await _productService.CreateProduct(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Product created", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("Comment")]
    public async Task<IActionResult> CreateComment([FromBody] Request.ProductCommentRequest request)
    {
        // Thêm ParentCommentId vào request nếu có, thì là reply luôn
        var result = await _productService.CreateComment(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Comment created", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("Post/{id}")]
    public async Task<IActionResult> UpdateProductPostingById(Guid id, [FromForm] Request.UpdateProductRequest request)
    {
        var result = await _productService.UpdateProductPostingById(id, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Product updated", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminOrSellerPolicy)]
    [HttpDelete("Post/{id}")]
    public async Task<IActionResult> SoftDeleteProductPostingById(Guid id)
    {
        var result = await _productService.SoftDeleteProductPostingById(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Product removed", HttpContext.TraceIdentifier));
    }
    
    
}