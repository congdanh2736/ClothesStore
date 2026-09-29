using AutoMapper;
using ClothesStore.Api.DTOs.CollectionTech;
using ClothesStore.Api.Models;

namespace ClothesStore
{
    public class CollectionTechProfile : Profile {
        public CollectionTechProfile()
        {
            CreateMap<CollectionTech,CollectionTechDto>();
            CreateMap<CreateCollectionTech,CollectionTech>();
        }
    }
}