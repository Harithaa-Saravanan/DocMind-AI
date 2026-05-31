export interface FileMeta {
    id: string;
    name: string;
    size: number;
    status: 'uploading' | 'processed' | 'error';
  }
  
  export interface Message {
    id: string;
    role: 'user' | 'assistant';
    content: string;
    timestamp: Date;
    citations?: string[]; // To hold document source names/page numbers later
  }
  
  export interface ChatSession {
    id: string;
    title: string;
    messages: Message[];
    uploadedFiles: FileMeta[];
  }