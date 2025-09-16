using AssetHierarchyAPI.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetHierarchyAPI.Application.Interfaces
{
    public interface IAssetLogRepository
    {
        Task AddAsync(AssetLog log);
        Task SaveChangesAsync();
    }
}
