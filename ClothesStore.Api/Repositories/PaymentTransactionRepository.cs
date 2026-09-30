using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothesStore.Api.Data;
using ClothesStore.Api.Interface.Repositories;
using ClothesStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClothesStore.Api.Repositories
{
    public class PaymentTransactionRepository : IPaymentMethodRepository
    {
        public readonly ApplicationDbContext _context ;
        public PaymentTransactionRepository(ApplicationDbContext context) => _context = context;
        public async Task AddAsync(PaymentTransaction paymentTransaction)
        {
            _context.PaymentTransactions.Add(paymentTransaction);
            await _context.SaveChangesAsync();

        }

        public async Task DeleteAsync(PaymentTransaction paymentTransaction)
        {
           _context.PaymentTransactions.Remove(paymentTransaction);
           await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<PaymentTransaction>> GetAllAsync()
            => await _context.PaymentTransactions
                .Include( p => p.Order)
                .ToListAsync();
           
        

        public async Task<PaymentTransaction?> GetByIdAsync(int id)
            => await _context.PaymentTransactions.FindAsync(id);

        public async Task<PaymentTransaction?> GetByIdWithDetailsAsync(int id)
            => await _context.PaymentTransactions
                .Include(transaction => transaction.Order)
                .FirstOrDefaultAsync(transaction => transaction.Id == id);

        public async Task UpdateAsync(PaymentTransaction paymentTransaction)
        {
            _context.PaymentTransactions.Update(paymentTransaction);
            await _context.SaveChangesAsync();
        }
    }
}
