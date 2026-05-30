using Docmind.Api.Models;
using DocMind.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using static Docmind.Api.Models.ChatModel;

namespace DocMind.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly RagChatService _chatService;

        // Inject our custom RAG chat worker via constructor
        public ChatController(RagChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("ask")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ChatResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AskQuestion([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("The question payload cannot be empty.");
            }

            try
            {
                // Trigger the core RAG execution loop
                var response = await _chatService.AskQuestionAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while generating a response: {ex.Message}");
            }
        }
    }
}