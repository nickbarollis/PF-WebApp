﻿using Fundraising.App.Core.Entities;
using Fundraising.App.Core.Interfaces;
using Fundraising.App.Core.Options;
using Fundraising.App.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fundraising.App.Core.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IApplicationDbContext dbContext;

        public PaymentService(IApplicationDbContext _dbContext)
        {
            dbContext = _dbContext;
        }

        public OptionPayment CreatePayment(OptionPayment optionPayment)
        {
            Payment payment = new()
            {
                CreditCard = optionPayment.CreditCard,
                Backer = optionPayment.Backer,
                Reward = optionPayment.Reward,
                PaymentDate = DateTime.Now
            };

            dbContext.Payments.Add(payment);
            dbContext.SaveChanges();

            return new OptionPayment(payment);
        }

        public bool DeletePayment(int Id)
        {
            Payment dbContexPayment = dbContext.Payments.Find(Id);
            if (dbContexPayment == null) return false;
            dbContext.Payments.Remove(dbContexPayment);
            return true;
        }

        public List<OptionPayment> ReadAllPayments()
        {
            List<Payment> payments = dbContext.Payments.ToList();
            List<OptionPayment> optionPayments = new();
            payments.ForEach(payment => optionPayments.Add(new OptionPayment(payment)));
            return optionPayments;
        }

    
    }
}
