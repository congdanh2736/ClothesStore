// [THÊM MỚI] Chatbot AI - kiểm tra câu hỏi gửi lên
using ClothesStore.Api.DTOs.Chatbot;
using FluentValidation;

namespace ClothesStore.Api.Validators.Chatbot
{
    public class ChatRequestValidator : AbstractValidator<ChatRequestDto>
    {
        public ChatRequestValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Bạn chưa nhập câu hỏi.")
                .MaximumLength(500).WithMessage("Câu hỏi tối đa 500 ký tự.");
        }
    }
}
