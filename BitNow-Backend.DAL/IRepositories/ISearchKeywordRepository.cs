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
    }
}
