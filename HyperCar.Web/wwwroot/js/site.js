// ===== HyperCar Site JS =====
// SignalR connection + notification bell (Admin & Customer)

(function () {
    'use strict';

    // Detect role from body attribute
    const userRole = document.body.dataset.userRole || '';
    const isAdmin = userRole === 'Admin';
    const isCustomer = userRole === 'Customer';

    // =============== NOTIFICATION BELL STYLES ===============
    const bellStyles = document.createElement('style');
    bellStyles.textContent = `
        /* Notification dropdown */
        .notification-dropdown {
            border: 1px solid rgba(255,107,53,0.3) !important;
            border-radius: 12px !important;
            box-shadow: 0 12px 40px rgba(0,0,0,0.5) !important;
            background: #1a1a2e !important;
            padding: 0 !important;
        }
        .notification-dropdown .notif-item {
            padding: 12px 16px;
            border-bottom: 1px solid rgba(255,255,255,0.06);
            cursor: pointer;
            transition: background 0.2s;
            display: flex;
            align-items: flex-start;
            gap: 10px;
        }
        .notification-dropdown .notif-item:hover {
            background: rgba(255,107,53,0.08);
        }
        .notification-dropdown .notif-item.unread {
            background: rgba(255,107,53,0.05);
            border-left: 3px solid #ff6b35;
        }
        .notification-dropdown .notif-item .notif-icon {
            font-size: 1.1rem;
            margin-top: 2px;
            flex-shrink: 0;
        }
        .notification-dropdown .notif-item .notif-content {
            flex: 1;
            min-width: 0;
        }
        .notification-dropdown .notif-item .notif-text {
            font-size: 0.85rem;
            color: #e0e0e0;
            line-height: 1.3;
            word-wrap: break-word;
        }
        .notification-dropdown .notif-item .notif-time {
            font-size: 0.7rem;
            color: #888;
            margin-top: 3px;
        }

        /* Bell ring animation */
        @keyframes bellRing {
            0% { transform: rotate(0); }
            10% { transform: rotate(14deg); }
            20% { transform: rotate(-12deg); }
            30% { transform: rotate(10deg); }
            40% { transform: rotate(-8deg); }
            50% { transform: rotate(6deg); }
            60% { transform: rotate(-4deg); }
            70% { transform: rotate(2deg); }
            80% { transform: rotate(0deg); }
            100% { transform: rotate(0deg); }
        }
        .bell-ring {
            animation: bellRing 0.8s ease;
            transform-origin: top center;
        }

        /* Badge bounce */
        @keyframes badgeBounce {
            0% { transform: translate(-50%, -50%) scale(1); }
            30% { transform: translate(-50%, -50%) scale(1.5); }
            60% { transform: translate(-50%, -50%) scale(0.9); }
            100% { transform: translate(-50%, -50%) scale(1); }
        }
        .badge-bounce {
            animation: badgeBounce 0.5s ease;
        }

        /* Notification toast */
        .notification-toast {
            background: linear-gradient(135deg, #1a1a2e, #16213e);
            border: 1px solid rgba(255,107,53,0.4);
            border-radius: 10px;
            padding: 14px 18px;
            margin-bottom: 8px;
            box-shadow: 0 6px 24px rgba(0,0,0,0.4);
            animation: slideIn 0.4s cubic-bezier(0.68, -0.55, 0.265, 1.55);
            max-width: 380px;
        }
        @keyframes slideIn {
            from { opacity: 0; transform: translateX(80px); }
            to { opacity: 1; transform: translateX(0); }
        }
    `;
    document.head.appendChild(bellStyles);

    // =============== SIGNALR CONNECTION ===============
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notification")
        .withAutomaticReconnect()
        .build();

    // =============== NOTIFICATION STATE ===============
    // Admin uses 'hypercar_admin_notifs', Customer uses 'hypercar_customer_notifs'
    const storageKey = isAdmin ? 'hypercar_admin_notifs' : 'hypercar_customer_notifs';
    let notifications = JSON.parse(localStorage.getItem(storageKey) || '[]');
    let unreadCount = notifications.filter(n => !n.read).length;

    function saveNotifications() {
        if (notifications.length > 50) {
            notifications = notifications.slice(0, 50);
        }
        localStorage.setItem(storageKey, JSON.stringify(notifications));
    }

    function updateBadge() {
        const badge = document.getElementById('notifBadge');
        if (!badge) return;
        unreadCount = notifications.filter(n => !n.read).length;
        badge.textContent = unreadCount;
        badge.style.display = unreadCount > 0 ? '' : 'none';
    }

    function getNotifIcon(type) {
        const icons = {
            'car': 'fas fa-car text-info',
            'brand': 'fas fa-tags text-warning',
            'order': 'fas fa-box text-accent',
            'account': 'fas fa-user text-primary',
            'role': 'fas fa-user-shield text-success',
            'payment': 'fas fa-credit-card text-success',
            'shipping': 'fas fa-truck text-info',
            'delivered': 'fas fa-box-open text-success',
            'completed': 'fas fa-flag-checkered text-success',
            'info': 'fas fa-info-circle text-info',
            'warning': 'fas fa-exclamation-triangle text-warning',
            'success': 'fas fa-check-circle text-success',
            'danger': 'fas fa-exclamation-circle text-danger'
        };
        return icons[type] || icons['info'];
    }

    function timeAgo(dateStr) {
        const now = new Date();
        const d = new Date(dateStr);
        const diffMs = now - d;
        const diffSec = Math.floor(diffMs / 1000);
        const diffMin = Math.floor(diffSec / 60);
        const diffHr = Math.floor(diffMin / 60);

        if (diffSec < 60) return 'Vừa xong';
        if (diffMin < 60) return `${diffMin} phút trước`;
        if (diffHr < 24) return `${diffHr} giờ trước`;
        return d.toLocaleDateString('vi-VN');
    }

    function renderNotifList() {
        const list = document.getElementById('notifList');
        if (!list) return;

        if (notifications.length === 0) {
            list.innerHTML = `
                <div class="text-center text-muted py-4">
                    <i class="fas fa-bell-slash fa-2x mb-2"></i>
                    <p class="mb-0 small">Chưa có thông báo</p>
                </div>`;
            return;
        }

        list.innerHTML = notifications.map((n, i) => `
            <div class="notif-item ${n.read ? '' : 'unread'}" data-index="${i}">
                <i class="${getNotifIcon(n.type)} notif-icon"></i>
                <div class="notif-content">
                    <div class="notif-text">${n.message}</div>
                    <div class="notif-time">${timeAgo(n.time)}</div>
                </div>
            </div>
        `).join('');

        // Click to mark as read
        list.querySelectorAll('.notif-item').forEach(item => {
            item.addEventListener('click', () => {
                const idx = parseInt(item.dataset.index);
                notifications[idx].read = true;
                saveNotifications();
                updateBadge();
                item.classList.remove('unread');
            });
        });
    }

    function addNotification(message, type) {
        const notif = {
            message: message,
            type: type || 'info',
            time: new Date().toISOString(),
            read: false
        };
        notifications.unshift(notif);
        saveNotifications();
        updateBadge();
        renderNotifList();

        // Ring the bell
        const bellIcon = document.querySelector('#bellToggle .fa-bell');
        if (bellIcon) {
            bellIcon.classList.remove('bell-ring');
            void bellIcon.offsetWidth;
            bellIcon.classList.add('bell-ring');
        }

        // Bounce the badge
        const badge = document.getElementById('notifBadge');
        if (badge) {
            badge.classList.remove('badge-bounce');
            void badge.offsetWidth;
            badge.classList.add('badge-bounce');
        }
    }

    // =============== CLEAR ALL BUTTON ===============
    document.addEventListener('click', function (e) {
        if (e.target.closest('#clearAllNotifs')) {
            notifications = [];
            saveNotifications();
            updateBadge();
            renderNotifList();
        }
    });

    // Mark all as read when opening dropdown
    const bellToggle = document.getElementById('bellToggle');
    if (bellToggle) {
        bellToggle.addEventListener('shown.bs.dropdown', function () {
            notifications.forEach(n => n.read = true);
            saveNotifications();
            setTimeout(() => updateBadge(), 500);
        });
    }

    // =============== SIGNALR EVENT HANDLERS ===============

    // Universal page handler registry — any page can push a handler
    window.__pageNotifHandlers = window.__pageNotifHandlers || [];

    if (isAdmin) {
        // Admin: receives all system notifications
        connection.on("ReceiveAdminNotification", (message, type) => {
            addNotification(message, type);
            showToast('Admin Alert', message, type === 'payment' ? 'success' : 'warning');

            // Dispatch to all registered page handlers
            window.__pageNotifHandlers.forEach(handler => {
                try { handler(message, type); } catch (e) { console.error('Page handler error:', e); }
            });
        });
    }

    if (isCustomer) {
        // Customer: receives their own notifications — bell + dispatch to page handlers
        connection.on("ReceiveCustomerNotification", (message, type) => {
            addNotification(message, type);

            // Dispatch to customer page handlers (e.g. Account Dashboard)
            window.__pageNotifHandlers.forEach(handler => {
                try { handler(message, type); } catch (e) { console.error('Page handler error:', e); }
            });
        });
    }

    const _statusVi = {
        'Pending': 'Chờ xác nhận', 'Confirmed': 'Đã xác nhận', 'Processing': 'Đang xử lý',
        'Shipping': 'Đang giao', 'Delivered': 'Đã giao', 'Completed': 'Hoàn thành',
        'Cancelled': 'Đã hủy', 'Refunded': 'Đã hoàn tiền'
    };

    // Both roles can receive these (SignalR targets specific user via Clients.User)
    connection.on("ReceiveOrderUpdate", (orderId, status, message) => {
        showToast(`Đơn hàng #${orderId} — ${_statusVi[status] || status}`, message, 'info');
    });

    connection.on("ReceivePaymentConfirmation", (orderId, status) => {
        if (isCustomer) addNotification(`Thanh toán đơn hàng #${orderId} thành công!`, 'payment');
        showToast(`Thanh toán ${status}`, `Đơn hàng #${orderId} đã thanh toán!`, 'success');
    });

    connection.on("ReceiveShippingUpdate", (orderId, status, trackingCode) => {
        const viStatus = _statusVi[status] || status;
        if (isCustomer) addNotification(`Đơn hàng #${orderId}: ${viStatus}${trackingCode ? ' — ' + trackingCode : ''}`, 'shipping');
        showToast('Cập nhật vận chuyển', `Đơn hàng #${orderId}: ${viStatus}`, 'info');
    });

    // Collection update — broadcast to all clients (public page)
    connection.on("ReceiveCollectionUpdate", (type) => {
        window.__pageNotifHandlers.forEach(handler => {
            try { handler('__collection_update__', type); } catch (e) { }
        });
    });

    connection.start()
        .then(() => {
            console.log('SignalR connected');
        })
        .catch(err => console.log('SignalR connection error:', err));

    // =============== TOAST NOTIFICATION ===============
    window.showToast = function (title, message, type = 'info') {
        const area = document.getElementById('notificationArea');
        if (!area) return;

        const iconMap = {
            success: 'fa-check-circle text-success',
            danger: 'fa-exclamation-circle text-danger',
            warning: 'fa-exclamation-triangle text-warning',
            info: 'fa-info-circle text-info'
        };

        const toast = document.createElement('div');
        toast.className = 'notification-toast';
        toast.innerHTML = `
            <div class="d-flex align-items-start gap-2">
                <i class="fas ${iconMap[type] || iconMap.info} mt-1"></i>
                <div>
                    <strong>${title}</strong>
                    <p class="mb-0 small text-muted">${message}</p>
                </div>
            </div>
        `;
        area.appendChild(toast);

        setTimeout(() => {
            toast.style.transition = 'all 0.4s ease-out';
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100px)';
            setTimeout(() => toast.remove(), 400);
        }, 5000);
    };

    // =============== INIT ===============
    updateBadge();
    renderNotifList();
})();
