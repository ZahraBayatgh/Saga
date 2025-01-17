﻿using EventBus.Abstractions;
using InventoryService.Data;
using InventoryService.Dtos;
using InventoryService.IntegrationEvents.Events;
using InventoryService.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace InventoryService.IntegrationEvents.EventHandling
{
    public class CreateProductIntegrationEventHandler : IIntegrationEventHandler<CreateProductIntegrationEvent>
    {
        private readonly ILogger<CreateProductIntegrationEventHandler> _logger;
        private readonly InventoryDbContext _context;
        private readonly IProductService _productService;
        private readonly IInventoryTransactionService _inventoryTransactionService;
        private readonly IEventBus _eventBus;

        public CreateProductIntegrationEventHandler(ILogger<CreateProductIntegrationEventHandler> logger,
            InventoryDbContext context,
            IProductService productService,
            IInventoryTransactionService inventoryTransactionService,
            IEventBus eventBus)
        {
            _logger = logger;
            _context = context;
            _productService = productService;
            _inventoryTransactionService = inventoryTransactionService;
            _eventBus = eventBus;
        }
        public async Task Handle(CreateProductIntegrationEvent @event)
        {
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Check CreateProductIntegrationEvent
                CheckCreateProductIntegrationEventInstance(@event);
                bool isCommit = false;

                // Create product
                var ProductRequestDto = new ProductRequestDto
                {
                    ProductName = @event.ProductName,
                    Count = @event.InitialOnHand
                };
                var createProductResponce = await _productService.CreateProductAsync(ProductRequestDto);

                if (createProductResponce.IsFailure)
                    throw new Exception("CreateProductIntegrationEvent is failure.");

                // Initial InventoryTransactionRequestDto
                var InventoryTransactionDto = new InventoryTransactionRequestDto(createProductResponce.Value.ProductId, @event.InitialOnHand, Models.InventoryType.In);

                // Add InventoryTransaction
                var inventoryTransactionResponse = await _inventoryTransactionService.CreateInventoryTransactionAsync(InventoryTransactionDto);
                if (inventoryTransactionResponse.IsSuccess)
                    isCommit = true;

                bool createProductStatus = false;
                if (isCommit)
                {
                    transaction.Commit();

                    createProductStatus = true;
                }
                else
                {
                    transaction.Rollback();
                }

                // Publish SalesResultIntegrationEvent
                await PublishResult(@event, createProductStatus);

            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"CreateProductIntegrationEvent is null. Exception detail:{ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Product {@event.ProductName} wan not created. Exception detail:{ex.Message}");

                // Publish SalesResultIntegrationEvent
                await PublishResult(@event, false);

                throw;
            }
        }

        private async Task PublishResult(CreateProductIntegrationEvent @event, bool createProductStatus)
        {
            // Publish SalesResultIntegrationEvent
            InventoryResultIntegrationEvent inventoryResultIntegrationEvent = new InventoryResultIntegrationEvent(@event.ProductId, createProductStatus, @event.CorrelationId);
            await _eventBus.PublishAsync(inventoryResultIntegrationEvent);
        }

        private static void CheckCreateProductIntegrationEventInstance(CreateProductIntegrationEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException("CreateProductIntegrationEvent is null.");

            if (@event.ProductId <= 0)
                throw new ArgumentNullException("CreateProductIntegrationEvent ProductId is invalid.");

            if (string.IsNullOrEmpty(@event.ProductName))
                throw new ArgumentNullException("SalesResultIntegrationEvent ProductName is null.");
        }
    }
}
