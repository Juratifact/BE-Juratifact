using Juratifact.Repository.Entity;
using Juratifact.Service.Category;
using Juratifact.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace Juratifact.API.Controller;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet("")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await  _categoryService.GetCategories();
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get categories successfully", HttpContext.TraceIdentifier));
    }


    [HttpGet("{parentId}/children")]
    public async Task<IActionResult> GetCategoriesByParentId(Guid parentId)
    {
        var result = await _categoryService.GetCategoriesByParentId(parentId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Get categories by parentId successfully", HttpContext.TraceIdentifier));
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCategory(Request.CreateCategoryRequest request)
    {
        var result = await _categoryService.CreateCategory(request);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Create category successfully", HttpContext.TraceIdentifier));
    }

    [HttpPut("update/{categoryId}")]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, Request.UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateCategory(categoryId, request);
        return  Ok(ApiResponseFactory.SuccessResponse(result, "Update category successfully", HttpContext.TraceIdentifier));
    }

    [HttpDelete("delete/{categoryId}")]
    public async Task<IActionResult> DeleteCategory(Guid categoryId)
    {
        var result = await _categoryService.DeleteCategory(categoryId);
        return Ok(ApiResponseFactory.SuccessResponse(result, "Delete category successfully", HttpContext.TraceIdentifier));
    }
}