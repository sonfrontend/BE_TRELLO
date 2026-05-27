using BE_ECOMMERCE.Data;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using BE_ECOMMERCE.DTOs.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
public class ProductController(ApplicationDbContext context, IConfiguration configuration) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string search = null,
        [FromQuery] string sortPrice = null)
    {
        try
        {
            // 1. Khởi tạo câu lệnh truy vấn LINQ từ Database
            var query = _context.Products.AsNoTracking().AsQueryable();

            // Lọc dữ liệu theo từ khóa tìm kiếm (Text hoặc Voice đã dịch thành chữ)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchKeyword = search.Trim().ToLower();

                // Lọc những sản phẩm có Tên hoặc Mô tả chứa từ khóa tìm kiếm
                query = query.Where(p => p.ProductName.ToLower().Contains(searchKeyword));
            }

            // Kéo dữ liệu đã lọc từ DB lên bộ nhớ RAM
            var rawProducts = await query.ToListAsync();

            // 2. Gom nhóm theo ProductCode (Ví dụ: 0108775)
            var groupedData = rawProducts
                .GroupBy(p => p.ProductCode)
                .Select(g =>
                {
                    // Chọn sản phẩm đầu tiên trong nhóm làm "Sản phẩm đại diện"
                    var defaultProduct = g.FirstOrDefault();

                    // Lọc ra các sản phẩm CÒN LẠI (bỏ qua cái đại diện) để đưa vào mảng con
                    var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                    return new
                    {
                        // --- THÔNG TIN CỦA SẢN PHẨM ĐẦU TIÊN ---
                        ArticleId = defaultProduct.ArticleId,
                        ProductCode = defaultProduct.ProductCode,
                        CategoryId = defaultProduct.CategoryId,
                        ProductName = defaultProduct.ProductName,
                        Price = defaultProduct.Price,
                        ImageUrl = defaultProduct.ImageUrl,
                        Description = defaultProduct.Description,
                        Size = defaultProduct.Size,     // Hiện rõ Size của cái đại diện
                        Color = defaultProduct.Color,   // Hiện rõ Màu của cái đại diện
                        StockQuantity = defaultProduct.StockQuantity,

                        // --- MẢNG CHỨA CÁC SẢN PHẨM LIÊN QUAN CÒN LẠI ---
                        Products = otherVariants.Select(v => new
                        {
                            ArticleId = v.ArticleId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity,
                            Price = v.Price,
                            ImageUrl = v.ImageUrl
                        }).OrderBy(v => v.Size).ToList()
                    };
                })
                .ToList();

            // Sắp xếp theo giá hoặc mặc định
            if (sortPrice == "asc")
            {
                groupedData = groupedData.OrderBy(p => p.Price).ToList();
            }
            else if (sortPrice == "desc")
            {
                groupedData = groupedData.OrderByDescending(p => p.Price).ToList();
            }
            else
            {
                groupedData = groupedData.OrderByDescending(p => p.ArticleId).ToList();
            }

            // 3. Xử lý phân trang
            var totalCount = groupedData.Count;
            var pagedProducts = groupedData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var hasMore = (page * pageSize) < totalCount;

            return Ok(new
            {
                data = pagedProducts,
                hasMore = hasMore,
                nextPage = hasMore ? page + 1 : (int?)null,
                totalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    [HttpGet("{productCode}")]
    public async Task<IActionResult> GetProductByCode(string productCode)
    {
        try
        {
            var variants = await _context.Products
                .Include(p => p.Categories)
                    .ThenInclude(c => c.ParentCategory)
                .Where(p => p.ProductCode == productCode)
                .AsNoTracking()
                .ToListAsync();

            if (!variants.Any())
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            var defaultProduct = variants.FirstOrDefault();

            var otherVariants = variants.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

            var result = new
            {
                // --- THÔNG TIN CỦA SẢN PHẨM ĐẦU TIÊN ---
                ArticleId = defaultProduct.ArticleId,
                ProductCode = defaultProduct.ProductCode,
                CategoryId = defaultProduct.CategoryId,
                CategoryName = defaultProduct.Categories?.Name ?? "Danh mục chung",
                ParentCategoryName = defaultProduct.Categories?.ParentCategory?.Name,
                ProductName = defaultProduct.ProductName,
                Price = defaultProduct.Price,
                ImageUrl = defaultProduct.ImageUrl,
                Description = defaultProduct.Description,
                Size = defaultProduct.Size,
                Color = defaultProduct.Color,
                StockQuantity = defaultProduct.StockQuantity,

                // --- MẢNG CHỨA CÁC SẢN PHẨM LIÊN QUAN CÒN LẠI ---
                Products = otherVariants.Select(v => new
                {
                    ArticleId = v.ArticleId,
                    Size = v.Size,
                    Color = v.Color,
                    StockQuantity = v.StockQuantity,
                    Price = v.Price,
                    ImageUrl = v.ImageUrl
                }).OrderBy(v => v.Size).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    [HttpGet("parent-category/{parentId}")]
    public async Task<IActionResult> GetProductsByParentCategory(
        int parentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortPrice = null)
    {
        try
        {
            var query = _context.Products
                .Include(p => p.Categories)
                .Where(p => p.Categories != null && p.Categories.ParentId == parentId)
                .AsNoTracking();

            var rawProducts = await query.ToListAsync();

            var groupedData = rawProducts
                .GroupBy(p => p.ProductCode)
                .Select(g =>
                {
                    var defaultProduct = g.FirstOrDefault();
                    var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                    return new
                    {
                        ArticleId = defaultProduct.ArticleId,
                        ProductCode = defaultProduct.ProductCode,
                        CategoryId = defaultProduct.CategoryId,
                        ProductName = defaultProduct.ProductName,
                        Price = defaultProduct.Price,
                        ImageUrl = defaultProduct.ImageUrl,
                        Description = defaultProduct.Description,
                        Size = defaultProduct.Size,
                        Color = defaultProduct.Color,
                        StockQuantity = defaultProduct.StockQuantity,

                        Products = otherVariants.Select(v => new
                        {
                            ArticleId = v.ArticleId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity,
                            Price = v.Price,
                            ImageUrl = v.ImageUrl
                        }).OrderBy(v => v.Size).ToList()
                    };
                })
                .ToList();

            if (sortPrice == "asc")
            {
                groupedData = groupedData.OrderBy(p => p.Price).ToList();
            }
            else if (sortPrice == "desc")
            {
                groupedData = groupedData.OrderByDescending(p => p.Price).ToList();
            }
            else
            {
                groupedData = groupedData.OrderByDescending(p => p.ArticleId).ToList();
            }

            var totalCount = groupedData.Count;
            var pagedProducts = groupedData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var hasMore = (page * pageSize) < totalCount;

            return Ok(new
            {
                data = pagedProducts,
                hasMore = hasMore,
                nextPage = hasMore ? page + 1 : (int?)null,
                totalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    // TÁCH RIÊNG API TÌM KIẾM
    // Đường dẫn: GET /api/Product/search?q={từ_khóa}&page=1&pageSize=15
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var queryable = _context.Products.AsQueryable();

        // 1. Lọc theo từ khóa (Nếu khách có gõ chữ)
        if (!string.IsNullOrWhiteSpace(q))
        {
            queryable = queryable.Where(p => p.ProductName.Contains(q));
        }

        // 2. Đếm tổng số kết quả
        int totalItems = await queryable.CountAsync();

        // 3. Phân trang
        var products = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ArticleId,
                p.ProductCode,
                p.ProductName,
                p.Price,
                p.ImageUrl
            })
            .ToListAsync();

        bool hasMore = (page * pageSize) < totalItems;

        return Ok(new
        {
            data = products,
            hasMore = hasMore,
            total = totalItems
        });
    }

    [HttpPost("search-by-image")]
    public async Task<IActionResult> SearchByImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("Vui lòng chọn hình ảnh.");
        }

        try
        {
            var aiServiceUrl = _configuration["AiServiceUrl"];
            if (string.IsNullOrEmpty(aiServiceUrl))
            {
                return StatusCode(500, "Chưa cấu hình AiServiceUrl.");
            }

            using var httpClient = new HttpClient();
            using var content = new MultipartFormDataContent();
            
            // Đọc file ảnh từ request
            using var stream = image.OpenReadStream();
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
            
            content.Add(streamContent, "image", image.FileName);

            // Gọi API Python
            var response = await httpClient.PostAsync($"{aiServiceUrl}/api/predict", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Lỗi từ AI Service: {errorMsg}");
            }

            // Nhận danh sách ArticleId từ Python
            var articleIds = await response.Content.ReadFromJsonAsync<List<string>>();

            if (articleIds == null || !articleIds.Any())
            {
                return Ok(new { data = new List<object>() });
            }

            // 1. Tìm các ProductCode tương ứng với ArticleId do AI trả về
            var aiProducts = await _context.Products
                .Where(p => articleIds.Contains(p.ArticleId))
                .Select(p => new { p.ArticleId, p.ProductCode })
                .ToListAsync();

            var productCodes = aiProducts.Select(p => p.ProductCode).Distinct().ToList();

            // 2. Lấy TẤT CẢ các biến thể của những ProductCode này (để hiển thị đủ size/màu)
            var rawProducts = await _context.Products
                .Where(p => productCodes.Contains(p.ProductCode))
                .ToListAsync();

            // 3. Gom nhóm theo ProductCode giống như API GetProducts
            var groupedData = rawProducts
                .GroupBy(p => p.ProductCode)
                .Select(g =>
                {
                    // Chọn sản phẩm đại diện: Là sản phẩm có độ tương đồng AI cao nhất (nằm ở đầu list articleIds)
                    var defaultProduct = g.OrderBy(p => 
                    {
                        var idx = articleIds.IndexOf(p.ArticleId);
                        return idx == -1 ? int.MaxValue : idx;
                    }).First();

                    var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                    return new
                    {
                        ArticleId = defaultProduct.ArticleId,
                        ProductCode = defaultProduct.ProductCode,
                        CategoryId = defaultProduct.CategoryId,
                        ProductName = defaultProduct.ProductName,
                        Price = defaultProduct.Price,
                        ImageUrl = defaultProduct.ImageUrl,
                        Description = defaultProduct.Description,
                        Size = defaultProduct.Size,
                        Color = defaultProduct.Color,
                        StockQuantity = defaultProduct.StockQuantity,

                        Products = otherVariants.Select(v => new
                        {
                            ArticleId = v.ArticleId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity,
                            Price = v.Price,
                            ImageUrl = v.ImageUrl
                        }).OrderBy(v => v.Size).ToList()
                    };
                })
                // Sắp xếp các nhóm dựa trên thứ tự trả về từ AI
                .OrderBy(g => 
                {
                    var idx = articleIds.IndexOf(g.ArticleId);
                    return idx == -1 ? int.MaxValue : idx;
                })
                .ToList();

            return Ok(new
            {
                data = groupedData,
                total = groupedData.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    [HttpPost("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromBody] RecommendRequestDto request)
    {
        try
        {
            if (request == null || request.ArticleIds == null || !request.ArticleIds.Any())
            {
                // Fallback: Nếu không có lịch sử, lấy random 15 sản phẩm
                var randomProducts = await _context.Products
                    .OrderBy(r => Guid.NewGuid())
                    .Take(request.TopK)
                    .ToListAsync();
                    
                var fallbackData = randomProducts
                    .GroupBy(p => p.ProductCode)
                    .Select(g => 
                    {
                        var defaultProduct = g.First();
                        var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                        return new
                        {
                            ArticleId = defaultProduct.ArticleId,
                            ProductCode = defaultProduct.ProductCode,
                            CategoryId = defaultProduct.CategoryId,
                            ProductName = defaultProduct.ProductName,
                            Price = defaultProduct.Price,
                            ImageUrl = defaultProduct.ImageUrl,
                            Description = defaultProduct.Description,
                            Size = defaultProduct.Size,
                            Color = defaultProduct.Color,
                            StockQuantity = defaultProduct.StockQuantity,
                            Products = otherVariants.Select(v => new
                            {
                                ArticleId = v.ArticleId,
                                Size = v.Size,
                                Color = v.Color,
                                StockQuantity = v.StockQuantity,
                                Price = v.Price,
                                ImageUrl = v.ImageUrl
                            }).OrderBy(v => v.Size).ToList()
                        };
                    })
                    .ToList();
                
                return Ok(new { data = fallbackData, total = fallbackData.Count });
            }

            // Gọi sang Python AI
            using (var httpClient = new HttpClient())
            {
                var payload = new
                {
                    article_ids = request.ArticleIds,
                    top_k = request.TopK
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("http://localhost:8000/api/recommend-by-history", content);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Lỗi từ AI Server");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var recommendedArticleIds = JsonSerializer.Deserialize<List<string>>(responseString);

                if (recommendedArticleIds == null || !recommendedArticleIds.Any())
                {
                    return Ok(new { data = new List<object>(), total = 0 });
                }

                var aiProducts = await _context.Products
                    .Where(p => recommendedArticleIds.Contains(p.ArticleId))
                    .Select(p => new { p.ArticleId, p.ProductCode })
                    .ToListAsync();

                var productCodes = aiProducts.Select(p => p.ProductCode).Distinct().ToList();

                var rawProducts = await _context.Products
                    .Where(p => productCodes.Contains(p.ProductCode))
                    .ToListAsync();

                var groupedData = rawProducts
                    .GroupBy(p => p.ProductCode)
                    .Select(g =>
                    {
                        var defaultProduct = g.OrderBy(p => 
                        {
                            var idx = recommendedArticleIds.IndexOf(p.ArticleId);
                            return idx == -1 ? int.MaxValue : idx;
                        }).First();

                        var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                        return new
                        {
                            ArticleId = defaultProduct.ArticleId,
                            ProductCode = defaultProduct.ProductCode,
                            CategoryId = defaultProduct.CategoryId,
                            ProductName = defaultProduct.ProductName,
                            Price = defaultProduct.Price,
                            ImageUrl = defaultProduct.ImageUrl,
                            Description = defaultProduct.Description,
                            Size = defaultProduct.Size,
                            Color = defaultProduct.Color,
                            StockQuantity = defaultProduct.StockQuantity,

                            Products = otherVariants.Select(v => new
                            {
                                ArticleId = v.ArticleId,
                                Size = v.Size,
                                Color = v.Color,
                                StockQuantity = v.StockQuantity,
                                Price = v.Price,
                                ImageUrl = v.ImageUrl
                            }).OrderBy(v => v.Size).ToList()
                        };
                    })
                    .OrderBy(g => 
                    {
                        var idx = recommendedArticleIds.IndexOf(g.ArticleId);
                        return idx == -1 ? int.MaxValue : idx;
                    })
                    .ToList();

                return Ok(new
                {
                    data = groupedData,
                    total = groupedData.Count
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    [HttpPost("by-ids")]
    public async Task<IActionResult> GetProductsByIds([FromBody] GetByIdsRequestDto request)
    {
        try
        {
            if (request == null || request.ArticleIds == null || !request.ArticleIds.Any())
            {
                return Ok(new { data = new List<object>(), total = 0 });
            }

            var aiProducts = await _context.Products
                .Where(p => request.ArticleIds.Contains(p.ArticleId))
                .Select(p => new { p.ArticleId, p.ProductCode })
                .ToListAsync();

            var productCodes = aiProducts.Select(p => p.ProductCode).Distinct().ToList();

            var rawProducts = await _context.Products
                .Where(p => productCodes.Contains(p.ProductCode))
                .ToListAsync();

            var groupedData = rawProducts
                .GroupBy(p => p.ProductCode)
                .Select(g =>
                {
                    var defaultProduct = g.OrderBy(p => 
                    {
                        var idx = request.ArticleIds.IndexOf(p.ArticleId);
                        return idx == -1 ? int.MaxValue : idx;
                    }).First();

                    var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                    return new
                    {
                        ArticleId = defaultProduct.ArticleId,
                        ProductCode = defaultProduct.ProductCode,
                        CategoryId = defaultProduct.CategoryId,
                        ProductName = defaultProduct.ProductName,
                        Price = defaultProduct.Price,
                        ImageUrl = defaultProduct.ImageUrl,
                        Description = defaultProduct.Description,
                        Size = defaultProduct.Size,
                        Color = defaultProduct.Color,
                        StockQuantity = defaultProduct.StockQuantity,

                        Products = otherVariants.Select(v => new
                        {
                            ArticleId = v.ArticleId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity,
                            Price = v.Price,
                            ImageUrl = v.ImageUrl
                        }).OrderBy(v => v.Size).ToList()
                    };
                })
                .ToList();

            if (request.SortPrice == "asc")
            {
                groupedData = groupedData.OrderBy(p => p.Price).ToList();
            }
            else if (request.SortPrice == "desc")
            {
                groupedData = groupedData.OrderByDescending(p => p.Price).ToList();
            }
            else
            {
                groupedData = groupedData.OrderBy(g => 
                {
                    var idx = request.ArticleIds.IndexOf(g.ArticleId);
                    return idx == -1 ? int.MaxValue : idx;
                }).ToList();
            }

            return Ok(new
            {
                data = groupedData,
                total = groupedData.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }
}