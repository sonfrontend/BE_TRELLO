using System.Collections.Generic;

namespace BE_ECOMMERCE.DTOs.Products
{
    public class GetByIdsRequestDto
    {
        public List<string> ArticleIds { get; set; } = new List<string>();
        public string SortPrice { get; set; }
    }
}
