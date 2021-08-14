﻿using CSharpFunctionalExtensions;
using InventoryService.Data;
using InventoryService.Dtos;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryService.Services
{
    public class InventoryTransactionService : IInventoryTransactionService
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<InventoryTransactionService> _logger;

        public InventoryTransactionService(InventoryDbContext context,
            ILogger<InventoryTransactionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// This method get latest invertory transaction by product id.
        /// If the input productId is not valid or an expiration occurs, a Failure will be returned.
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<Result<int>> GetLatestInventoryTransactionByProductIdAsync(int productId)
        {
            try
            {
                // Check product id
                if (productId <= 0)
                    return Result.Failure<int>($"Product id is invalid.");

                // Get latast InventoryTransaction by product id
                var inventoryTransaction = await _context.InventoryTransactions.OrderByDescending(x => x.Id).FirstOrDefaultAsync(x => x.ProductId == productId);

                // Check inventoryTransaction
                if (inventoryTransaction == null)
                    return Result.Failure<int>($"Get latest inventory transaction by product id {productId} was not found.");

                return Result.Success(inventoryTransaction.CurrentCount);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Get latest inventory transaction by product id {productId} was failed. Exception detail:{ex.Message}");
                return Result.Failure<int>($"et latest inventory transaction by product id {productId} was failed.");
            }
        }

        /// <summary>
        /// This method add invertory transaction by product id.
        /// If the input inventoryTransactionDto is not valid or an expiration occurs, a Failure will be returned. 
        /// </summary>
        /// <param name="inventoryTransactionDto"></param>
        /// <returns></returns>
        public async Task<Result<InventoryTransaction>> CreateInventoryTransactionAsync(InventoryTransactionDto inventoryTransactionDto)
        {
            try
            {
                // Check inventoryTransactionDto instance
                var inventoryTransactionDtoValidation = CheckInventoryTransactionInstance(inventoryTransactionDto);
                if (inventoryTransactionDtoValidation.IsFailure)
                    return Result.Failure<InventoryTransaction>(inventoryTransactionDtoValidation.Error);

                // Intialize InventoryTransaction
                var inventoryTransaction = new InventoryTransaction
                {
                    ProductId = inventoryTransactionDto.ProductId,
                    Type = InventoryType.Out,
                    ChangeCount = inventoryTransactionDto.ChangeCount,
                    CurrentCount = inventoryTransactionDto.CurrentCount
                };

                // Add InventoryTransaction
                await _context.InventoryTransactions.AddAsync(inventoryTransaction);
                await _context.SaveChangesAsync();

                return Result.Success(inventoryTransaction);

            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Add inventory transaction failed. Exception detail:{ex.Message}");
                return Result.Failure<InventoryTransaction>("Add inventory transaction failed. Exception detail");
            }
        }

        /// <summary>
        /// This method delete a InventoryTransaction to the table.
        /// If the input inventoryTransactionId is not valid or an expiration occurs, a Failure will be returned.
        /// </summary>
        /// <param name="inventoryTransactionId"></param>
        /// <returns></returns>
        public async Task<Result> DeleteInventoryTransactionAsync(int inventoryTransactionId)
        {
            try
            {
                // Check inventoryTransaction id
                if (inventoryTransactionId <= 0)
                    return Result.Failure($"InventoryTransaction id is zero.");

                // Get inventoryTransaction by inventoryTransaction id
                var inventoryTransaction = await _context.InventoryTransactions.FirstOrDefaultAsync(x => x.Id == inventoryTransactionId);
                if (inventoryTransaction == null)
                    return Result.Failure($"InventoryTransaction id is invalid.");

                // Remove inventoryTransaction
                _context.InventoryTransactions.Remove(inventoryTransaction);
                await _context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Delete inventoryTransaction with {inventoryTransactionId} id failed. Exception detail:{ex.Message}");

                return Result.Failure($"Delete inventoryTransaction with {inventoryTransactionId} id failed.");
            }
        }

        /// <summary>
        /// This methode check a inventoryTransactionDto instance
        /// </summary>
        /// <param name="inventoryTransactionDto"></param>
        /// <returns></returns>
        private static Result CheckInventoryTransactionInstance(InventoryTransactionDto inventoryTransactionDto)
        {
            if (inventoryTransactionDto == null)
                return Result.Failure("InventoryTransactionDto instance is invalid.");

            if (inventoryTransactionDto.ProductId <= 0)
                return Result.Failure("InventoryTransaction ProductId is invalid.");

            if (inventoryTransactionDto.ChangeCount <= 0)
                return Result.Failure("InventoryTransaction ChangeCount is invalid.");

            if (inventoryTransactionDto.CurrentCount <= 0)
                return Result.Failure("InventoryTransaction CurrentCount is invalid.");

            return Result.Success();
        }
    }
}
