import { useState, useRef } from 'react';
import { Message, FileMeta } from './types/chat';
import { apiService } from './services/api'; // Import our new service layer

export default function App() {
  const [messages, setMessages] = useState<Message[]>([
    {
      id: '1',
      role: 'assistant',
      content: 'Hello! Upload your documents on the left sidebar, and ask me anything about their contents.',
      timestamp: new Date()
    }
  ]);
  const [input, setInput] = useState('');
  const [files, setFiles] = useState<FileMeta[]>([]);
  const [isTyping, setIsTyping] = useState(false); // Loader tracking state
  
  const fileInputRef = useRef<HTMLInputElement>(null);

  // --- HANDLER: SEND CHAT PROMPT ---
  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isTyping) return;

    const userMessage: Message = {
      id: Date.now().toString(),
      role: 'user',
      content: input,
      timestamp: new Date()
    };

    // Update UI instantly with user input
    setMessages((prev) => [...prev, userMessage]);
    const currentInput = input;
    setInput('');
    setIsTyping(true);

    try {
      // 🚀 Fire real network request to C# RAG backend
      const data = await apiService.sendChatMessage(currentInput, [...messages, userMessage]);
      
      const aiMessage: Message = {
        id: Date.now().toString(),
        role: 'assistant',
        content: data.answer,
        timestamp: new Date(),
        citations: data.citations
      };
      setMessages((prev) => [...prev, aiMessage]);
    } catch (error) {
      console.error("Chat Error:", error);
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now().toString(),
          role: 'assistant',
          content: 'Sorry, I encountered an error communicating with the local intelligence engine. Is the backend running?',
          timestamp: new Date()
        }
      ]);
    } finally {
      setIsTyping(false);
    }
  };

  // --- HANDLER: INGEST SOURCE FILES ---
  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = e.target.files;
    if (!selectedFiles) return;

    Array.from(selectedFiles).forEach(async (file) => {
      if (file.size > 10 * 1024 * 1024) {
        setFiles((prev) => [...prev, { id: Math.random().toString(), name: file.name, size: file.size, status: 'error' }]);
        return;
      }

      const tempId = Math.random().toString();
      const initialFileMeta: FileMeta = { id: tempId, name: file.name, size: file.size, status: 'uploading' };
      setFiles((prev) => [...prev, initialFileMeta]);

      try {
        // 🚀 Upload real file streaming bits to backend
        await apiService.uploadDocument(file);
        
        setFiles((current) =>
          current.map((item) => (item.id === tempId ? { ...item, status: 'processed' } : item))
        );
      } catch (error) {
        console.error("Upload Error:", error);
        setFiles((current) =>
          current.map((item) => (item.id === tempId ? { ...item, status: 'error' } : item))
        );
      }
    });
  };

  const onUploadContainerClick = () => fileInputRef.current?.click();

  return (
    <div className="flex h-screen w-screen bg-slate-950 text-slate-100 overflow-hidden font-sans">
      {/* SIDEBAR */}
      <aside className="w-80 border-r border-slate-800 bg-slate-900/50 flex flex-col justify-between">
        <div className="p-4 flex flex-col gap-6">
          <div>
            <h1 className="text-xl font-bold tracking-tight bg-gradient-to-r from-cyan-400 to-blue-500 bg-clip-text text-transparent">DocMind AI</h1>
            <p className="text-xs text-slate-400 mt-1">Intelligent Document RAG Workspace</p>
          </div>

          <input type="file" ref={fileInputRef} onChange={handleFileChange} multiple accept=".pdf,.txt,.docx" className="hidden" />

          <div onClick={onUploadContainerClick} className="border border-dashed border-slate-700 bg-slate-950/40 rounded-xl p-6 text-center hover:border-cyan-500/50 hover:bg-slate-900/40 transition cursor-pointer group">
            <span className="text-xs text-slate-400 block group-hover:text-slate-200 transition">Click to select knowledge base documents</span>
            <span className="text-[10px] text-slate-500 block mt-1">PDF, TXT, DOCX (Max 10MB)</span>
          </div>

          <div className="flex flex-col gap-2">
            <h3 className="text-xs font-semibold uppercase tracking-wider text-slate-500">Active Corpus Documents</h3>
            {files.length === 0 ? (
              <div className="text-xs text-slate-500 italic p-2 bg-slate-950/20 rounded-lg">No source documents uploaded yet.</div>
            ) : (
              <div className="flex flex-col gap-1.5 max-h-64 overflow-y-auto pr-1">
                {files.map((file) => (
                  <div key={file.id} className="flex items-center justify-between p-2.5 bg-slate-900 border border-slate-800 rounded-lg text-xs">
                    <div className="truncate max-w-[160px]" title={file.name}>{file.name}</div>
                    <div>
                      {file.status === 'uploading' && <span className="text-amber-400 font-medium animate-pulse">Processing...</span>}
                      {file.status === 'processed' && <span className="text-emerald-400 font-medium">Ready</span>}
                      {file.status === 'error' && <span className="text-rose-500 font-medium">Failed</span>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
        <div className="p-4 border-t border-slate-800 text-center text-xs text-slate-500">Status: Interface Wired to Network</div>
      </aside>

      {/* CONVERSATION AREA */}
      <main className="flex-1 flex flex-col justify-between bg-slate-950">
        <section className="flex-1 overflow-y-auto p-6 flex flex-col gap-4">
          {messages.map((msg) => (
            <div key={msg.id} className={`flex flex-col max-w-[75%] p-4 rounded-2xl gap-1 text-sm ${msg.role === 'user' ? 'bg-blue-600 text-white self-end rounded-tr-none' : 'bg-slate-900 border border-slate-800 text-slate-200 self-start rounded-tl-none'}`}>
              <span className="font-medium text-xs opacity-75">{msg.role === 'user' ? 'You' : 'DocMind Agent'}</span>
              <p className="leading-relaxed">{msg.content}</p>
              {msg.citations && msg.citations.length > 0 && (
                <div className="mt-2 pt-2 border-t border-slate-800/60 flex flex-wrap gap-1">
                  {msg.citations.map((cite, idx) => (
                    <span key={idx} className="text-[10px] bg-slate-950 px-2 py-0.5 rounded text-cyan-400 border border-slate-800">📌 {cite}</span>
                  ))}
                </div>
              )}
            </div>
          ))}
          {isTyping && (
            <div className="bg-slate-900 border border-slate-800 text-slate-400 self-start p-4 rounded-2xl rounded-tl-none text-xs italic animate-pulse">
              DocMind Agent is thinking and searching documents...
            </div>
          )}
        </section>

        <footer className="p-4 bg-gradient-to-t from-slate-950 via-slate-950 to-transparent">
          <form onSubmit={handleSendMessage} className="max-w-4xl mx-auto flex gap-2 bg-slate-900 border border-slate-800 p-2 rounded-xl focus-within:border-cyan-500/50 transition">
            <input type="text" value={input} onChange={(e) => setInput(e.target.value)} placeholder="Query your operational knowledge base..." className="flex-1 bg-transparent px-3 py-2 text-sm text-slate-100 outline-none placeholder-slate-500" disabled={isTyping} />
            <button type="submit" disabled={isTyping} className="bg-cyan-600 hover:bg-cyan-500 disabled:bg-slate-800 disabled:text-slate-500 text-slate-950 px-4 py-2 rounded-lg font-semibold text-xs tracking-wide transition">
              {isTyping ? 'Thinking...' : 'Send'}
            </button>
          </form>
        </footer>
      </main>
    </div>
  );
}