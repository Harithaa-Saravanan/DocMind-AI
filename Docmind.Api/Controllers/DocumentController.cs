using DocMind.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Threading.Tasks;

namespace Docmind.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly DocumentIngestionService _ingestionService;

        // Inject our custom ingestion worker via constructor
        public DocumentController(DocumentIngestionService ingestionService)
        {
            _ingestionService = ingestionService;
        }

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded or the file is empty.");
            }

            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid file type. Only PDF documents are supported.");
            }

            try
            {
                // Open the file stream and send it to our pipeline
                using var stream = file.OpenReadStream();
                int totalChunks = await _ingestionService.IngestPdfAsync(stream, file.FileName);

                return Ok(new
                {
                    Message = "Document processed and stored successfully.",
                    FileName = file.FileName,
                    ChunksCreated = totalChunks
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during processing: {ex.Message}");
            }
        }        
    }
}