using System;
using System.Collections.Generic;
using System.Text;

namespace ExampleMicroservice
{
    public enum ServiceType
    {
        Customer,
        Order
    }

    public class ExampleServiceOptions
    {
        public ServiceType ServiceName { get; set; } = ServiceType.Customer;
    }
}
