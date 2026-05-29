# DocMind 🧠 — Chat with Your Documents

DocMind is a self-hosted, private Retrieval-Augmented Generation (RAG) platform. It allows teams or individual users to upload business documents (like PDFs) and have natural, interactive conversations with that content securely, privately, and completely offline.

---

## 🚀 Key Value Propositions

* **100% Data Sovereignty:** Every document text snippet, vector embedding calculation, and AI inference cycle stays entirely on your local machine. No third-party APIs (like OpenAI) are used.
* **Zero Infrastructure Cost:** Built completely using free, open-source local models and framework engines.
* **Traceable Answers:** Eliminates AI hallucinations by forcing the model to cite the exact source file and text segment it used to formulate its response.

---

### Technical Stack Component Registry

| Component | Technology | Responsibility |
| :--- | :--- | :--- |
| **React App** | React + Vite | User interface — upload files, display chat and source citations. |
| **DocumentController** | ASP.NET Core | Handles file upload multipart HTTP requests. |
| **ChatController** | ASP.NET Core | Handles question/answer prompt HTTP endpoints. |
| **DocumentIngestionService** | C# + iTextSharp | Extracts raw text, orchestrates chunking, embeddings, and database pushes. |
| **RagChatService** | C# + Semantic Kernel | Vectorizes user queries, fetches relevant text, constructs the system context prompt. |
| **EmbeddingService** | Semantic Kernel | Interface mapping to convert textual blocks into numerical vector representations. |
| **QdrantService** | Qdrant.Client | Direct database interface to map custom entities into Qdrant collection payloads. |
| **Ollama (llama3.2)** | Ollama (Local) | Local text LLM that generates natural language answers strictly from context boundaries. |
| **Ollama (nomic-embed-text)**| Ollama (Local) | Local text embedding generator (outputs 768-dimensional mathematical arrays). |
| **Qdrant** | Qdrant (Docker) | High-performance, persistent vector database server with native HNSW indexing. |

---

## 🔄 Core Application Workflows

### Flow 1 — Document Ingestion (One-time Setup)
1. **React:** User selects file. Axios posts file to `/api/documents/upload` via `FormData`.
2. **DocumentController:** Receives and validates multipart request, hands to `DocumentIngestionService`.
3. **DocumentIngestionService:** Extracts raw document strings using `iTextSharp`.
4. **DocumentIngestionService:** Slices text into 500-token chunks with 50-token semantic overlap to preserve context across boundaries.
5. **EmbeddingService:** Dispatches text chunks to local Ollama instance running `nomic-embed-text` (Free).
6. **QdrantService:** Stores chunk text string, metadata (file name, page number), and vector arrays permanently.
7. **DocumentController:** Confirms ingestion with a `200 OK` status, returning metadata arrays.
8. **React:** Appends document data to the sidebar registry.

### Flow 2 — Question Answering Loop (RAG)
1. **React:** User submits question string to `/api/chat/ask`.
2. **ChatController:** Validates structure, routes payload to `RagChatService`.
3. **RagChatService:** Encodes user's question into a 768-dimension coordinate vector using local `nomic-embed-text`.
4. **QdrantService:** Queries collection via cosine similarity match and retrieves the top 5 most relevant document chunks.
5. **RagChatService:** Constructs augmented system prompt injection binding retrieved text blocks to strict operational rules.
6. **Ollama (llama3.2):** Compiles the response based solely on the provided context limits.
7. **RagChatService:** Pulls provenance parameters (File Name, Page) from vector block metadata arrays.
8. **ChatController:** Returns structural JSON containing `{ answer, sources }`.
9. **React:** Hydrates the UI with responsive bubbles containing clickable markdown source citations.

---

## 🔌 API Specification

### Document Management
* `POST /api/documents/upload` - Upload file object (`FormData`). Returns `{ id, name, chunks }`.
* `GET /api/documents` - Fetches array listing available indexed files.
* `DELETE /api/documents/{id}` - Drops target elements from the vector namespace.

### Conversation Loops
* `POST /api/chat/ask` - Submits `{ question, conversationId }`. Returns `{ answer, sources: [{ documentName, pageNumber }] }`.
* `GET /api/chat/history/{id}` - Returns historical array matching `{ role, content }` to preserve UI state.
* `DELETE /api/chat/history/{id}` - Flushes active context state blocks.

---

## 🔒 Security Configuration Standards

* **Zero Key Leaks:** No cloud keys or variables live inside the client React build.
* **Encrypted Traffic Channels:** Local infrastructure maps direct connection bindings using native ASP.NET `UseHttpsRedirection` pipelines.
* **Runtime File Validation:** Input controls strictly parse binary signatures (MIME) and constrain incoming file sizing to 15MB limits.
* **Zero Hallucination Prompts:** System prompts apply explicit negative boundary constraints ("If you cannot find the answer, reply only with 'I could not find relevant information in your documents'").

---

## 🛠️ Local Installation & Launch

### Prerequisites
1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download)
2. Install [Ollama](https://ollama.com/) and download the models:
   ```bash
   ollama pull llama3.2
   ollama pull nomic-embed-text
3. Install Docker Desktop
