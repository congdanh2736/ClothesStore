using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothesStore.Api.DTOs.PaymentTransaction
{
    public class UpdatePaymentTransactionRequest
    {
        public string PaymentMethod { get; set; } = string.Empty;
        
    }
}