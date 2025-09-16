//using AssetHierarchyAPI.Application.DTOs;
//using AssetHierarchyAPI.Application.Interfaces;
//using AssetHierarchyAPI.Domain.Models;
//using AssetHierarchyAPI.Infrastructure.Data;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Infrastructure.Repositories
//{
//    public class SignalRepository : ISignalRepository
//    {
//        private readonly AppDbContext _context;

//        public SignalRepository(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IEnumerable<Signal>> GetByAssetAsync(int assetId)
//        {
//            return await _context.Signals
//                .AsNoTracking()
//                .Where(x => x.AssetId == assetId)
//                .OrderBy(x => x.AssetId)
//                .ToListAsync();
//        }

//        public async Task<Signal?> GetByIdAsync(int id)
//        {
//            return await _context.Signals
//                .AsNoTracking()
//                .FirstOrDefaultAsync(x => x.Id == id);
//        }
//        public async Task<Signal> AddSignalAsync(int assetId, GlobalSignalDTO signals)
//        {
//            _context.Signals.Add(signal);
//            await _context.SaveChangesAsync();
//        }

//    }
//}
