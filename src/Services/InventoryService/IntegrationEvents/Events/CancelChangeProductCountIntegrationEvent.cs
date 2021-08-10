﻿using EventBus.Events;

namespace InventoryService.IntegrationEvents.Events
{
    public class CancelChangeProductCountIntegrationEvent : IntegrationEvent
    {
        public CancelChangeProductCountIntegrationEvent(string name, int count)
        {
            Name = name;
            Count = count;
        }
        public string Name { get; set; }
        public int Count { get; set; }
    }
}