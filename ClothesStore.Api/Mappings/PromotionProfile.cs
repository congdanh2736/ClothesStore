using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClothesStore.Api.DTOs.Promotion;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, PromotionDto>();

            CreateMap<CreatePromotionRequest, Promotion>();
            CreateMap<UpdatePromotionRequest, Promotion>();

        }
        
    }
}
