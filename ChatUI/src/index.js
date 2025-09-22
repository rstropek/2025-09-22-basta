import './index.css';
import { marked } from 'marked';

class ChatBot {
    constructor() {
        this.conversationId = null;
        this.messageInput = document.getElementById('message-input');
        this.sendButton = document.getElementById('send-button');
        this.conversationHistory = document.getElementById('conversation-history');
        this.status = document.getElementById('status');
        
        this.setupEventListeners();
        this.initializeConversation();
    }
    
    setupEventListeners() {
        this.sendButton.addEventListener('click', () => this.sendMessage());
        
        this.messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    }
    
    async initializeConversation() {
        try {
            this.setStatus('Initializing conversation...');
            
            const response = await fetch(`${__SERVICE_BASE__}/conversations/`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            this.conversationId = data.conversationId;
            this.setStatus('Ready to chat!');
            this.messageInput.focus();
            
        } catch (error) {
            console.error('Failed to initialize conversation:', error);
            this.setStatus('Failed to initialize conversation. Please refresh the page.');
        }
    }
    
    async sendMessage() {
        const message = this.messageInput.value.trim();
        if (!message || !this.conversationId) return;
        
        // Disable input while processing
        this.setInputEnabled(false);
        
        // Add user message to conversation
        this.addMessage(message, 'user');
        
        // Clear input
        this.messageInput.value = '';
        
        try {
            this.setStatus('Sending message...');
            
            const response = await fetch(`${__SERVICE_BASE__}/conversations/${this.conversationId}/chat`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ message: message })
            });
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            // Handle server-sent events for streaming response
            await this.handleStreamingResponse(response);
            
        } catch (error) {
            console.error('Failed to send message:', error);
            this.addMessage('Sorry, there was an error processing your message. Please try again.', 'assistant');
            this.setStatus('Error occurred. Ready to try again.');
        } finally {
            this.setInputEnabled(true);
            this.messageInput.focus();
        }
    }
    
    async handleStreamingResponse(response) {
        this.setStatus('Assistant is typing...');
        
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        
        // Create assistant message element
        const assistantMessageElement = this.createMessageElement('', 'assistant');
        const contentElement = assistantMessageElement.querySelector('.message-content');
        
        let fullMessageText = '';
        
        try {
            let buffer = '';
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                buffer += decoder.decode(value, { stream: true });
                
                // Process complete SSE messages
                const lines = buffer.split('\n');
                buffer = lines.pop(); // Keep incomplete line in buffer
                
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.slice(6); // Remove "data: " prefix
                        if (data === '[DONE]') {
                            // Render final markdown content
                            contentElement.innerHTML = marked.parse(fullMessageText);
                            this.setStatus('Ready to chat!');
                            return;
                        }
                        
                        try {
                            const messageData = JSON.parse(data);
                            if (messageData.deltaText) {
                                fullMessageText += messageData.deltaText;
                                // Show plain text during streaming, will be converted to markdown when complete
                                contentElement.textContent = fullMessageText;
                                this.scrollToBottom();
                            }
                        } catch (parseError) {
                            console.warn('Failed to parse SSE data:', parseError);
                        }
                    }
                }
            }
        } finally {
            reader.releaseLock();
            // Ensure markdown is rendered even if stream ends unexpectedly
            if (fullMessageText) {
                contentElement.innerHTML = marked.parse(fullMessageText);
            }
            this.setStatus('Ready to chat!');
        }
    }
    
    addMessage(content, sender) {
        const messageElement = this.createMessageElement(content, sender);
        this.conversationHistory.appendChild(messageElement);
        this.scrollToBottom();
    }
    
    createMessageElement(content, sender) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${sender}-message`;
        
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        
        // Render markdown for assistant messages, plain text for user messages
        if (sender === 'assistant' && content) {
            contentDiv.innerHTML = marked.parse(content);
        } else {
            contentDiv.textContent = content;
        }
        
        messageDiv.appendChild(contentDiv);
        
        // If it's an assistant message and we're adding it during streaming, append to history
        if (sender === 'assistant' && content === '') {
            this.conversationHistory.appendChild(messageDiv);
        }
        
        return messageDiv;
    }
    
    setInputEnabled(enabled) {
        this.messageInput.disabled = !enabled;
        this.sendButton.disabled = !enabled;
    }
    
    setStatus(message) {
        this.status.textContent = message;
    }
    
    scrollToBottom() {
        this.conversationHistory.scrollTop = this.conversationHistory.scrollHeight;
    }
}

// Initialize the chatbot when the DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new ChatBot();
});