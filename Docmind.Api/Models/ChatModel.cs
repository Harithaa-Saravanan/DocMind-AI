namespace Docmind.Api.Models
{
    public class ChatModel
    {
        // Represents a single text bubble request from the user
        public class ChatRequest
        {
            public string Question { get; set; } = string.Empty;
            public string ConversationId { get; set; } = string.Empty;
        }

        // The structured response object compiled by our RAG engine
        public class ChatResponse
        {
            public string Answer { get; set; } = string.Empty;
            public List<SourceCitation> Sources { get; set; } = new();
            public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        }

        // Individual citation details mapping where the text context came from
        public class SourceCitation
        {
            public string DocumentName { get; set; } = string.Empty;
            public int PageNumber { get; set; }
            public string RelevantExcerpt { get; set; } = string.Empty;
        }
    }
}
