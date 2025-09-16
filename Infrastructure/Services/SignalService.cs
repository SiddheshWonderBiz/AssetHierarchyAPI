using AssetHierarchyAPI.Application.DTOs;
using AssetHierarchyAPI.Application.Interfaces;
using AssetHierarchyAPI.Domain.Models;
using AssetHierarchyAPI.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssetHierarchyAPI.Infrastructure.Services
{
    public class SignalService : ISignalServices
    {
        private readonly ISignalRepository _repository;
        private readonly ILoggingServiceDb _loggerdb;
        private readonly IHubContext<NotificationHub> _hubContext;

        private static readonly HashSet<string> allowed =
            new(StringComparer.OrdinalIgnoreCase) { "int", "string", "real" };

        public SignalService(ISignalRepository repository, ILoggingServiceDb loggerdb, IHubContext<NotificationHub> hubContext)
        {
            _repository = repository;
            _loggerdb = loggerdb;
            _hubContext = hubContext;
        }

        public async Task<IEnumerable<Signal>> GetByAssetAsync(int assetId)
        {
            return await _repository.GetByAssetAsync(assetId);
        }

        public async Task<Signal?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Signal> AddSignalAsync(int assetId, GlobalSignalDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "Signal cannot be null");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name cannot be empty");

            if (string.IsNullOrWhiteSpace(dto.ValueType))
                throw new ArgumentException("ValueType cannot be empty");

            if (dto.Name.Length > 50)
                throw new ArgumentException("Name can't be greater than 50 characters");

            if (dto.Description.Length > 300)
                throw new ArgumentException("Description can't be greater than 300 characters");

            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            if (!Regex.IsMatch(dto.Name, pattern))
                throw new ArgumentException("Invalid name pattern: only a-z, 0-9, -, _ allowed");

            if (!allowed.Contains(dto.ValueType))
                throw new ArgumentException(
                    $"Invalid value type '{dto.ValueType}'. Allowed: {string.Join(", ", allowed)}"
                );

            if (await _repository.ExistsAsync(assetId, dto.Name))
                throw new InvalidOperationException($"Signal '{dto.Name}' already exists for this asset.");

            var signal = new Signal
            {
                Name = dto.Name,
                ValueType = dto.ValueType,
                Description = dto.Description,
                AssetId = assetId
            };

            await _repository.AddAsync(signal);

            await _loggerdb.LogsActionsAsync("Signal added", signal.Name);
            await _hubContext.Clients.All.SendAsync("signalAdded", $"Signal {signal.Name} added under Asset {assetId}");

            return signal;
        }

        public async Task<bool> UpdateSignalAsync(int id, GlobalSignalDTO dto)
        {
            var signal = await _repository.GetByIdAsync(id);
            if (signal == null)
                throw new InvalidOperationException($"Signal with id {id} not found.");

            if (dto == null)
                throw new ArgumentException("Signal cannot be null");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Signal name is required");

            if (string.IsNullOrWhiteSpace(dto.ValueType))
                throw new ArgumentException("Signal value type is required");

            string pattern = @"^[a-zA-Z0-9_\-\s]+$";
            if (!Regex.IsMatch(dto.Name, pattern))
                throw new ArgumentException("Invalid name pattern: only a-z, 0-9, -, _ allowed");

            if (!allowed.Contains(dto.ValueType))
                throw new ArgumentException(
                    $"Invalid value type '{dto.ValueType}'. Allowed: {string.Join(", ", allowed)}"
                );

            if (await _repository.ExistsAsync(signal.AssetId, dto.Name, signal.Id))
                throw new InvalidOperationException($"Signal '{dto.Name}' already exists for this asset.");

            signal.Name = dto.Name;
            signal.ValueType = dto.ValueType;
            signal.Description = dto.Description;

            await _repository.UpdateAsync(signal);
            await _loggerdb.LogsActionsAsync("Signal updated", signal.Name);

            return true;
        }

        public async Task<bool> DeleteSignalAsync(int id)
        {
            var signal = await _repository.GetByIdAsync(id);
            if (signal == null)
                throw new InvalidOperationException($"Signal with id {id} not found.");

            await _repository.DeleteAsync(signal);
            await _loggerdb.LogsActionsAsync("Signal deleted", signal.Name);

            return true;
        }
    }
}
