using Docmind.Api.Models;
using Qdrant.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Docmind.Api.Models.ChatModel;

namespace DocMind.Api.Services
{
    public class RagChatService
    {
        private readonly QdrantClient _qdrantClient;
        private const string CollectionName = "DocMindMemory";

        public RagChatService(QdrantClient qdrantClient)
        {
            _qdrantClient = qdrantClient;
        }

        public async Task<ChatResponse> AskQuestionAsync(ChatRequest request)
        {
            // 1. Vectorize the incoming user question string using Ollama
            var questionVector = await GenerateLocalEmbeddingAsync(request.Question);

            // 2. Perform raw similarity vector search on Qdrant, requesting top 4 results
            var searchResults = await _qdrantClient.SearchAsync(
                collectionName: CollectionName,
                vector: questionVector,
                limit: 2
            );

            // 3. Extract text payloads and build citations array
            var contextBuilder = new StringBuilder();
            var sourceCitations = new List<SourceCitation>();

            foreach (var point in searchResults)
            {
                var payload = point.Payload;

                // ✅ FIXED: Using .StringValue to read native gRPC text wrappers cleanly
                string chunkText = payload.ContainsKey("text") ? payload["text"].StringValue : "";
                string docName = payload.ContainsKey("documentName") ? payload["documentName"].StringValue : "Unknown";

                // ✅ FIXED: Extracting the 64-bit integer directly using .IntegerValue
                int pageNum = 0;
                if (payload.ContainsKey("pageNumber"))
                {
                    pageNum = (int)payload["pageNumber"].IntegerValue;
                }

                contextBuilder.AppendLine($"--- Source Document: {docName} (Page {pageNum}) ---");
                contextBuilder.AppendLine(chunkText);
                contextBuilder.AppendLine();

                sourceCitations.Add(new SourceCitation
                {
                    DocumentName = docName,
                    PageNumber = pageNum,
                    RelevantExcerpt = chunkText.Length > 150 ? chunkText.Substring(0, 150) + "..." : chunkText
                });
            }

            // 4. Construct strict RAG system context rules prompt
            string systemPrompt = $@"You are a helpful assistant answering questions based ONLY on the provided document context.
Rules:
- Only use information from the context below.
- If the answer is not contained within the context, say exactly: 'I could not find relevant information in your documents.'
- Always mention which document your answer comes from.
- Be concise and precise.

Context:
{contextBuilder}

Question: {request.Question}";

            // 5. Submit context prompt to local llama3.2 instance
            string aiAnswer = await CallLocalLlmAsync(systemPrompt);

            return new ChatResponse
            {
                Answer = aiAnswer,
                Sources = sourceCitations
            };
        }

        private async Task<float[]> GenerateLocalEmbeddingAsync(string text)
        {
            using var client = new HttpClient();
            var requestPayload = new { model = "nomic-embed-text", prompt = text };
            var response = await client.PostAsJsonAsync("http://localhost:11434/api/embeddings", requestPayload);
            response.EnsureSuccessStatusCode();

            var jsonResult = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
            return jsonResult?.Embedding ?? throw new InvalidOperationException("Embedding generation failed.");
        }

        private async Task<string> CallLocalLlmAsync(string prompt)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10); // Extend timeout window for slower local CPUs/GPUs

            var requestPayload = new
            {
                model = "llama3.2",
                prompt = prompt,
                stream = false // Keep execution simple without streaming logic for now
            };

            var response = await client.PostAsJsonAsync("http://localhost:11434/api/generate", requestPayload);
            response.EnsureSuccessStatusCode();

            var jsonResult = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
            return jsonResult?.Response ?? "I could not find relevant information in your documents.";
        }

        private class OllamaEmbeddingResponse { public float[]? Embedding { get; set; } }
        private class OllamaGenerateResponse { public string? Response { get; set; } }
    }
}