using AssetHierarchyAPI.Application.DTOs;
using AssetHierarchyAPI.Application.Interfaces;
using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class SignalRepository : ISignalRepository
    {
        private readonly AppDbContext _context;

        public SignalRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Signal>> GetByAssetAsync(int assetId)
        {
            return await _context.Signals
                .AsNoTracking()
                .Where(x => x.AssetId == assetId)
                .OrderBy(x => x.AssetId)
                .ToListAsync();
        }

        public async Task<Signal?> GetByIdAsync(int id)
        {
            return await _context.Signals
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }
        public async Task AddAsync(Signal signal)
        {
            _context.Signals.Add(signal);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Signal signal)
        {
            _context.Signals.Update(signal);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Signal signal)
        {
            _context.Signals.Remove(signal);
            await _context.SaveChangesAsync();
        }
        public async Task<bool> ExistsAsync(int assetId, string signalName, int? excludeId = null)
        {
            return await _context.Signals
                .AnyAsync(s => s.AssetId == assetId &&
                               s.Name.ToLower() == signalName.ToLower() &&
                               (!excludeId.HasValue || s.Id != excludeId.Value));
        }
    }
}
