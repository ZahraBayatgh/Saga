﻿using EventBus.Abstractions;
using InventoryService.Data;
using InventoryService.IntegrationEvents.Events;
using InventoryService.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace InventoryService.IntegrationEvents.EventHandling
{
    public class DeleteInventoryIntegrationEventHandler : IIntegrationEventHandler<DeleteInventoryIntegrationEvent>
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<DeleteInventoryIntegrationEventHandler> _logger;
        private readonly IInventoryTransactionService _inventoryTransactionService;
        private readonly IProductService _productService;

        public DeleteInventoryIntegrationEventHandler(ILogger<DeleteInventoryIntegrationEventHandler> logger,
            InventoryDbContext context,
            IProductService productService,
            IInventoryTransactionService inventoryTransactionService)
        {
            _logger = logger;
            _context = context;
            _productService = productService;
            _inventoryTransactionService = inventoryTransactionService;
        }
        public async Task Handle(DeleteInventoryIntegrationEvent @event)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Check CreateProductIntegrationEvent
                CheckDeleteInventoryIntegrationEventInstance(@event);

                bool isCommit = false;

                // Get and Check product in db
                var getProduct = await _productService.GetProductByNameAsync(@event.ProductName);
                if (getProduct.IsFailure)
                    throw new ArgumentNullException(getProduct.Error);

                // Delete product
                var createProductResponce = await _productService.DeleteProductAsync(getProduct.Value.Id);

                if (createProductResponce.IsSuccess)
                {
                    var getInventoryTransactions = await _inventoryTransactionService.GetInventoryTransactionsByProductIdAsync(getProduct.Value.Id);
                    if (getInventoryTransactions.IsSuccess)
                    {
                        var inventoryTransactionResponse = await _inventoryTransactionService.DeleteInventoryTransactionAsync(getInventoryTransactions.Value.Id);
                        if (inventoryTransactionResponse.IsSuccess)
                            isCommit = true;

                    }
                }


                if (isCommit)
                {
                    transaction.Commit();
                }
                else
                {
                    transaction.Rollback();
                }
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"CreateProductIntegrationEvent is null. Exception detail:{ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Product {@event.ProductName} wan not created. Exception detail:{ex.Message}");
                throw;
            }
        }

        private static void CheckDeleteInventoryIntegrationEventInstance(DeleteInventoryIntegrationEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException("CreateProductIntegrationEvent is null.");

            if (string.IsNullOrEmpty(@event.ProductName))
                throw new ArgumentNullException("SalesResultIntegrationEvent ProductName is null.");
        }
    }
}
