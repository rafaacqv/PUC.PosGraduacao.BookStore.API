﻿using Microsoft.Extensions.Configuration;
using PUC.PosGraduacao.BookStore.Domain.Interfaces;
using PUC.PosGraduacao.BookStore.Domain.Interfaces.Repositories;
using PUC.PosGraduacao.BookStore.Domain.Interfaces.Services;
using PUC.PosGraduacao.BookStore.Domain.Models;
using PUC.PosGraduacao.BookStore.Domain.Models.Order;
using Stripe;

namespace PUC.PosGraduacao.BookStore.Services.Services
{
  public class PaymentService : IPaymentService
  {
    private readonly IBasketRepository _basketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public PaymentService(IBasketRepository basketRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
      _basketRepository = basketRepository;
      _unitOfWork = unitOfWork;
      _configuration = configuration;
    }

    public async Task<CustomerBasket> CreateOrUpdatePaymentIntent(string basketId)
    {
      StripeConfiguration.ApiKey = _configuration["StripeSettings:SecretKey"];
      var basket = await _basketRepository.GetBasketAsync(basketId);
      var shippingPrice = 0m;

      if(basket.DeliveryMethodId.HasValue)
      {
        var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>()
          .GetByIdAsync((int)basket.DeliveryMethodId);
        shippingPrice = deliveryMethod.Price;
      }

      foreach(var item in basket.Items)
      {
        var productItem = await _unitOfWork.Repository<Domain.Models.Product>().GetByIdAsync(item.Id);
        if(item.Price != productItem.Price)
        {
          item.Price = productItem.Price;
        }
      }

      var service = new PaymentIntentService();
      PaymentIntent intent;

      if(string.IsNullOrEmpty(basket.PaymentIntentId))
      {
        var options = new PaymentIntentCreateOptions
        {
          Amount = (long)basket.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100,
          Currency = "usd",
          PaymentMethodTypes = new List<string> { "card" }
        };

        intent = await service.CreateAsync(options);
        basket.PaymentIntentId = intent.Id;
        basket.ClientSecret = intent.ClientSecret;
      }
      else
      {
        var options = new PaymentIntentUpdateOptions
        {
          Amount = (long)basket.Items.Sum(i => i.Quantity * (i.Price * 100)) + (long)shippingPrice * 100
        };
        await service.UpdateAsync(basket.PaymentIntentId, options);
      }

      await _basketRepository.UpdateBasketAsync(basket);
      return basket;
    }
  }
}
