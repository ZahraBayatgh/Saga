﻿using EventBus.Events;

namespace ProductCatalogService.IntegrationEvents.Events
{
    public class ResultInventoryIntegrationEvent : IntegrationEvent
    {
        public ResultInventoryIntegrationEvent(int productId, bool isSuccess)
        {
            ProductId = productId;
            IsSuccess = isSuccess;
        }

        public int ProductId { get; private set; }
        public bool IsSuccess { get; private set; }
    }
}
