using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface ISearchKeywordRepository
    {
        Task AddAsync(SearchKeyword keyword);

        /// <summary>
        /// Lấy danh sách các từ khóa tìm kiếm gần đây của một user.
        /// </summary>
        /// <param name="userId">Id người dùng.</param>
        /// <param name="take">Số lượng bản ghi muốn lấy.</param>
        Task<List<SearchKeyword>> GetRecentByUserAsync(int userId, int take = 20);
    }
}
