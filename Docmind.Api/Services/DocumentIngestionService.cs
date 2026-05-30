using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Docmind.Api.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using UglyToad.PdfPig;

namespace DocMind.Api.Services
{
    public class DocumentIngestionService
    {
        private readonly QdrantClient _qdrantClient;
        private const string CollectionName = "DocMindMemory";

        // Inject the direct, native QdrantClient wrapper
        public DocumentIngestionService(QdrantClient qdrantClient)
        {
            _qdrantClient = qdrantClient;
        }

        public async Task<int> IngestPdfAsync(Stream pdfStream, string fileName)
        {
            var documentId = Guid.NewGuid().ToString();
            var rawChunks = new List<(string Text, int PageNumber)>();

            // 1. Extract Raw Layout Text via PdfPig completely offline
            using (var pdfDocument = PdfDocument.Open(pdfStream))
            {
                foreach (var page in pdfDocument.GetPages())
                {
                    string pageText = page.Text;
                    if (string.IsNullOrWhiteSpace(pageText)) continue;

                    // Slice page text into logical windows
                    var pageChunks = ChunkTextWithOverlap(pageText, 500, 50);
                    foreach (var chunkText in pageChunks)
                    {
                        rawChunks.Add((chunkText, page.Number));
                    }
                }
            }

            // 2. Ensure the Collection exists in Qdrant with 768 dimensions (Nomic Embed size)
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (!collections.Contains(CollectionName))
            {
                await _qdrantClient.CreateCollectionAsync(CollectionName,
                    new VectorParams { Size = 768, Distance = Distance.Cosine });
            }

            var points = new List<PointStruct>();
            int index = 0;
            var uploadedAt = DateTime.UtcNow.ToString("o"); // ISO 8601 format string

            // 3. Process, compute vectors, and map directly to Qdrant native Points
            foreach (var chunk in rawChunks)
            {
                var embeddingVector = await GenerateLocalEmbeddingAsync(chunk.Text);
                var pointId = Guid.NewGuid();

                var point = new PointStruct
                {
                    Id = pointId,
                    Vectors = embeddingVector, // Direct float[] array mapping
                    Payload =
                    {
                        { "documentId", documentId },
                        { "documentName", fileName },
                        { "text", chunk.Text },
                        { "chunkIndex", index++ },
                        { "pageNumber", chunk.PageNumber },
                        { "uploadedAt", uploadedAt }
                    }
                };

                points.Add(point);
            }

            // 4. Batch upsert the points straight into our Docker database container
            if (points.Count > 0)
            {
                await _qdrantClient.UpsertAsync(CollectionName, points);
            }

            return points.Count;
        }

        private List<string> ChunkTextWithOverlap(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrEmpty(text)) return chunks;

            int currentIndex = 0;
            while (currentIndex < text.Length)
            {
                int length = Math.Min(chunkSize, text.Length - currentIndex);
                string chunk = text.Substring(currentIndex, length);
                chunks.Add(chunk);

                currentIndex += (chunkSize - overlap);
                if (currentIndex >= text.Length || length < chunkSize) break;
            }

            return chunks;
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

        private class OllamaEmbeddingResponse
        {
            public float[]? Embedding { get; set; }
        }
    }
}