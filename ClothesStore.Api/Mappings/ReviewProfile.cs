using AutoMapper;
using ClothesStore.Api.DTOs.Review;
using ClothesStore.Api.Models;

namespace ClothesStore.Api.Mappings
{
    public class ReviewProfile : Profile
    {
        public ReviewProfile()
        {
            CreateMap<Review, ReviewDto>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null
                        ? $"{src.Customer.FirstName} {src.Customer.LastName}"
                        : null))
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : null))
                .ForMember(dest => dest.ImageUrls,
                    opt => opt.MapFrom(src => src.Images != null
                        ? src.Images.Select(i => i.ImageUrl).ToList()
                        : new List<string>()));

            CreateMap<CreateReviewDto, Review>()
                .ForMember(dest => dest.Images, opt => opt.Ignore());
        }
    }
}
