// ===== AI Chatbot Widget =====
// Sends messages to /api/chat and displays responses with clickable product links

(function () {
    'use strict';

    const toggle = document.getElementById('chatToggle');
    const chatWindow = document.getElementById('chatWindow');
    const closeBtn = document.getElementById('chatClose');
    const input = document.getElementById('chatInput');
    const sendBtn = document.getElementById('chatSend');
    const messages = document.getElementById('chatMessages');

    if (!toggle) return;

    toggle.addEventListener('click', () => {
        const isVisible = chatWindow.style.display !== 'none';
        chatWindow.style.display = isVisible ? 'none' : 'flex';
        if (!isVisible) {
            input.focus();
            if (messages.children.length === 0) {
                addAiMessage("Xin chào anh/chị! 🏎️ Em là tư vấn viên AI của **HyperCar** — showroom siêu xe hàng đầu. Anh/chị muốn tìm hiểu về mẫu xe nào? Em có thể tư vấn về giá cả, thông số kỹ thuật, so sánh các dòng xe, hay bất kỳ điều gì anh/chị quan tâm ạ!");
            }
        }
    });

    closeBtn?.addEventListener('click', () => {
        chatWindow.style.display = 'none';
    });

    sendBtn?.addEventListener('click', sendMessage);
    input?.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') sendMessage();
    });

    async function sendMessage() {
        const text = input.value.trim();
        if (!text) return;

        addUserMessage(text);
        input.value = '';
        input.disabled = true;
        sendBtn.disabled = true;

        // Show typing indicator
        const typingEl = document.createElement('div');
        typingEl.className = 'ai-msg typing-indicator';
        typingEl.innerHTML = '<span class="typing-dots"><span>●</span><span>●</span><span>●</span></span>';
        messages.appendChild(typingEl);
        messages.scrollTop = messages.scrollHeight;

        try {
            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: text, sessionId: getSessionId() })
            });

            const result = await response.json();
            typingEl.remove();
            addAiMessage(result.aiResponse || 'Xin lỗi, em gặp chút trục trặc. Anh/chị thử lại nhé!');
        } catch (err) {
            typingEl.remove();
            addAiMessage('Kết nối bị gián đoạn. Anh/chị vui lòng thử lại ạ! 🔄');
        }

        input.disabled = false;
        sendBtn.disabled = false;
        input.focus();
        messages.scrollTop = messages.scrollHeight;
    }

    function addUserMessage(text) {
        const div = document.createElement('div');
        div.className = 'user-msg';
        div.textContent = text;
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
    }

    function addAiMessage(text) {
        const div = document.createElement('div');
        div.className = 'ai-msg';
        div.innerHTML = renderMarkdown(text);
        messages.appendChild(div);
        messages.scrollTop = messages.scrollHeight;
        return div;
    }

    /**
     * Converts limited markdown to safe HTML:
     * - [text](url) → clickable <a> links
     * - **bold** → <strong>
     * - *italic* → <em>
     * - Newlines → <br>
     * All other HTML is escaped for XSS prevention.
     */
    function renderMarkdown(text) {
        if (!text) return '';

        // Escape HTML first (XSS prevention)
        let html = text
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');

        // Markdown links: [text](url) → <a> tags (only allow safe URLs)
        html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, function (match, linkText, url) {
            // Only allow relative URLs and https URLs (no javascript:, data:, etc.)
            if (url.startsWith('/') || url.startsWith('https://')) {
                return `<a href="${url}" target="_blank" class="chat-link">${linkText}</a>`;
            }
            return linkText;
        });

        // Bold: **text** → <strong>
        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

        // Italic: *text* → <em>  (but not inside already processed **)
        html = html.replace(/(?<!\*)\*([^*]+)\*(?!\*)/g, '<em>$1</em>');

        // Newlines → <br>
        html = html.replace(/\n/g, '<br>');

        return html;
    }

    function getSessionId() {
        let sid = localStorage.getItem('hypercar_chat_session');
        if (!sid) {
            sid = 'sess_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            localStorage.setItem('hypercar_chat_session', sid);
        }
        return sid;
    }
})();
