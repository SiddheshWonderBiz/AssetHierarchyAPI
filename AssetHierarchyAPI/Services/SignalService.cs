using AssetHierarchyAPI.Controllers;
using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Interfaces;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetHierarchyAPI.Services
{
    public class SignalService : ISignalServices
    {

        private readonly AppDbContext _context;
        private static readonly HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase) { "int" , "string" , "real"};
        public SignalService(AppDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Signals> GetByAsset(int assetId)
        {
            return _context.Signals.AsNoTracking().Where(x => x.AssetId == assetId).OrderBy(x => x.AssetId).ToList();
        }
        public Signals? GetById(int id)
        {
            return _context.Signals.AsNoTracking().FirstOrDefault(x => x.Id == id);
        }
        private bool DuplicateSignal(GlobalSignalDTO dto, int assetId)
        {
            return _context.Signals
                .Any(s => s.AssetId == assetId && s.Name.ToLower() == dto.Name.ToLower());
        }

        public Signals AddSignal(int assetId, GlobalSignalDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Signal cannot be null");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name cannot be empty");

            if (string.IsNullOrWhiteSpace(dto.ValueType))
                throw new ArgumentException("ValueType cannot be empty");

            if (!allowed.Contains(dto.ValueType))
                throw new ArgumentException(
                    $"Invalid value type '{dto.ValueType}'. Allowed: {string.Join(", ", allowed)}"
                );

            // ✅ Correct: check asset exists
            var assetExists = _context.AssetNodes.Any(a => a.Id == assetId);
            if (!assetExists)
                throw new InvalidOperationException($"Asset with id {assetId} not found.");

            // ✅ Check duplicates
            if (DuplicateSignal(dto, assetId))
                throw new InvalidOperationException($"Signal with name '{dto.Name}' already exists for this asset.");

            // ✅ Map DTO → Entity
            var signal = new Signals
            {
                Name = dto.Name,
                ValueType = dto.ValueType,
                Description = dto.Description,
                AssetId = assetId
            };

            _context.Signals.Add(signal);
            _context.SaveChanges();

            return signal;
        }

        public bool UpdateSignal(int id, GlobalSignalDTO dto)
        {
            // 1. Find existing signal
            var signal = _context.Signals.FirstOrDefault(s => s.Id == id);
            if (signal == null)
                throw new InvalidOperationException($"Signal with id {id} not found.");

            // 2. Validate DTO
            if (dto == null)
                throw new ArgumentException("Signal cannot be null.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Signal name is required.");

            if (string.IsNullOrWhiteSpace(dto.ValueType))
                throw new ArgumentException("Signal value type is required.");

            if (!allowed.Contains(dto.ValueType))
                throw new ArgumentException(
                    $"Invalid value type '{dto.ValueType}'. Allowed: {string.Join(", ", allowed)}"
                );

            // 3. Check duplicates (same asset, different id)
            var duplicate = _context.Signals
                .Any(s => s.AssetId == signal.AssetId &&
                          s.Name.ToLower() == dto.Name.ToLower() &&
                          s.Id != id);

            if (duplicate)
                throw new InvalidOperationException($"Signal with name '{dto.Name}' already exists for this asset.");

            // 4. Update values
            signal.Name = dto.Name;
            signal.ValueType = dto.ValueType;
            signal.Description = dto.Description;

            // 5. Save
            _context.SaveChanges();
            return true;
        }
        public bool DeleteSignal(int id)
        {
            var signal = _context.Signals.FirstOrDefault(s => s.Id == id);
            if (signal == null)
                throw new InvalidOperationException($"Signal with id {id} not found.");

            _context.Signals.Remove(signal);
            _context.SaveChanges();
            return true;
        }





    }
}
