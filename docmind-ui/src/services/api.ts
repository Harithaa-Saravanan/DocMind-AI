import axios from 'axios';
import { Message } from '../types/chat';

// Update this port to match your exact local ASP.NET Core running URL (e.g., 5000, 5101, etc.)
const BASE_URL = 'http://localhost:5093/api'; 

const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const apiService = {
  /**
   * Targets your existing checked-in DocumentController
   */
  uploadDocument: async (file: File): Promise<{ message: string; fileName: string; chunksCreated: number }> => {
    const formData = new FormData();
    formData.append('file', file);

    // Changed path from /documents/upload to /Document/upload to match your [controller] route
    const response = await axios.post(`${BASE_URL}/Document/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  /**
   * Targets your existing checked-in ChatController
   */
  sendChatMessage: async (prompt: string, history: Message[]): Promise<{ answer: string; citations: string[] }> => {
    // Aligned payload object structure with your C# ChatRequest contract (binding to 'Question')
    const payload = {
      question: prompt, 
      chatHistory: history.map(msg => ({
        role: msg.role,
        content: msg.content
      }))
    };

    // Changed path from /chat/query to /Chat/ask to match your [controller] route
    const response = await apiClient.post('/Chat/ask', payload);
    return response.data;
  }
};