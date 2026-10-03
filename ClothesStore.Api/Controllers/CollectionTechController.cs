using ClothesStore.Api.DTOs.CollectionTech;
using ClothesStore.Api.Interface.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothesStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionTechController : ControllerBase
    {
        private readonly ICollectionTechService _service;
        public CollectionTechController(ICollectionTechService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<List<CollectionTechDto>>> GetAll()
        {
            var CollectionTech = await _service.GetAllAsync();
            return Ok(CollectionTech);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollectionTechDto>> GetById(int id)
        {
            var CollectionTech = await _service.GetByIdAsync(id);
            if (CollectionTech == null)
                return NotFound(new {message = "không tìm thấy bộ sư tập và công nghệ"});
            
            return Ok(CollectionTech);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCollectionTechDto dto)
        {
            var (success, error, data) = await _service.CreateAsync(dto);
            if (success == false)
                return BadRequest(new {message = error});
            return CreatedAtAction(nameof(GetById), new {id = data!.Id}, data );
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCollectionTechDto dto)
        {
            var (success,error) = await _service.UpdateAsync(id,dto);
                if (success == false) 
                    return BadRequest(new {message = error});

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var CollectionTech = await _service.DeleteAsync(id);
            if (CollectionTech == false)
                return NotFound(new {message = "không tìm thấy bộ sưu tập và công nghệ"});
            
            return NoContent();
        }
    }
}