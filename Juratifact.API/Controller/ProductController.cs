using Juratifact.API.Extensions;
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
    
    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("postings")]
    public async Task<IActionResult> CreateProduct([FromForm] ProductRequest.CreateProductRequest request)
    {
        var result = await _productService.CreateProduct(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Product created", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPost("comments")]
    public async Task<IActionResult> CreateComment([FromBody] ProductRequest.ProductCommentRequest request)
    {
        // Thêm ParentCommentId vào request nếu có, thì là reply luôn
        var result = await _productService.CreateComment(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Comment created", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.BuyerPolicy)]
    [HttpPut("postings/{id}")]
    public async Task<IActionResult> UpdateProductPostingById(Guid id, [FromForm] ProductRequest.UpdateProductRequest request)
    {
        var result = await _productService.UpdateProductPostingById(id, request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Product updated", HttpContext.TraceIdentifier));
    }

    [Authorize(Policy = JwtExtensions.AdminOrSellerPolicy)]
    [HttpDelete("postings/{id}")]
    public async Task<IActionResult> SoftDeleteProductPostingById(Guid id)
    {
        var result = await _productService.SoftDeleteProductPostingById(id);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Product removed", HttpContext.TraceIdentifier));
    }
}