﻿using EventBus.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using SaleService.DomainEvents.Events;
using SaleService.Dtos;
using SaleService.IntegrationEvents.Events;
using SaleService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaleService.DomainEvents.EventHandling
{
    public class UpdateProductDomainEventHandler : INotificationHandler<UpdateProductDomainEvent>
    {
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<UpdateProductDomainEventHandler> _logger;

        public UpdateProductDomainEventHandler(IProductService productService,
            IOrderService orderService ,
            IEventBus eventBus,
            ILogger<UpdateProductDomainEventHandler> logger)
        {
            _productService = productService;
            _orderService = orderService;
            _eventBus = eventBus;
            _logger = logger;
        }
        public async Task Handle(UpdateProductDomainEvent @event, CancellationToken cancellationToken)
        {
            try
            {
                // Intialize UpdateProductCountDto
                UpdateProductCountDto updateProductCountDto = new UpdateProductCountDto
                {
                    Name = @event.Name,
                    DecreaseCount = @event.DecreaseCount
                };
                // Update product count in product table
                var product = await _productService.UpdateProductCountAsync(updateProductCountDto);
                if (product.IsSuccess)
                {
                    UpdateProductCountAndAddInventoryTransaction updateProductIntegrationEvent = new UpdateProductCountAndAddInventoryTransaction(updateProductCountDto.Name, updateProductCountDto.DecreaseCount);
                    _eventBus.Publish(updateProductIntegrationEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Update product {@event.Name} has been Canceled. Exception detail:{ex.Message}");
               await _orderService.DeleteOrderAsync(@event.OrderId);
                throw;
            }
        }
    }
}