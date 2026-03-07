// ===== Cart JavaScript =====
// Handles add-to-cart with car animation, update, remove via AJAX

(function () {
    'use strict';

    // Helper: get antiforgery token from the global hidden input or any form
    function getToken() {
        // Prefer global token injected in _Layout
        const globalToken = document.getElementById('__AjaxAntiForgeryToken');
        if (globalToken) return globalToken.value;
        // Fallback to any form token
        const formToken = document.querySelector('input[name="__RequestVerificationToken"]');
        return formToken ? formToken.value : '';
    }

    // ===== Inject CSS for cart animation =====
    const style = document.createElement('style');
    style.textContent = `
        .flying-car {
            position: fixed;
            z-index: 99999;
            pointer-events: none;
        }
        .flying-car img {
            width: 80px;
            height: 50px;
            object-fit: contain;
            filter: drop-shadow(0 4px 12px rgba(255,107,53,0.6));
        }
        .flying-car .car-icon-fallback {
            font-size: 2.5rem;
            color: #ff6b35;
            filter: drop-shadow(0 4px 12px rgba(255,107,53,0.6));
        }
        @keyframes cartPulse {
            0% { transform: scale(1); }
            40% { transform: scale(1.8); background: #ff6b35; }
            100% { transform: scale(1); }
        }
        .cart-pulse { animation: cartPulse 0.5s ease; }
        @keyframes cartShake {
            0%, 100% { transform: rotate(0deg); }
            15% { transform: rotate(-15deg); }
            30% { transform: rotate(12deg); }
            45% { transform: rotate(-10deg); }
            60% { transform: rotate(8deg); }
            75% { transform: rotate(-5deg); }
        }
        .cart-shake { animation: cartShake 0.6s ease; }
        .add-to-cart-btn.added {
            background: #198754 !important;
            border-color: #198754 !important;
            pointer-events: none;
        }
        .cart-toast {
            position: fixed;
            top: 90px;
            right: 20px;
            z-index: 99998;
            background: linear-gradient(135deg, #1a1a2e, #16213e);
            border: 1px solid #ff6b35;
            border-radius: 12px;
            padding: 16px 24px;
            color: #fff;
            box-shadow: 0 8px 32px rgba(255,107,53,0.3);
            display: flex;
            align-items: center;
            gap: 12px;
            max-width: 380px;
            transform: translateX(120%);
            transition: transform 0.4s cubic-bezier(0.68, -0.55, 0.265, 1.55);
        }
        .cart-toast.show { transform: translateX(0); }
        .cart-toast .toast-icon { font-size: 1.5rem; color: #198754; }
        .cart-toast .toast-body h6 { margin: 0 0 2px; font-size: 0.9rem; color: #ff6b35; }
        .cart-toast .toast-body p { margin: 0; font-size: 0.8rem; color: #ccc; }
        .cart-particle {
            position: fixed;
            width: 6px;
            height: 6px;
            border-radius: 50%;
            pointer-events: none;
            z-index: 99998;
        }
    `;
    document.head.appendChild(style);

    // ===== Add to Cart with Animation =====
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.add-to-cart-btn');
        if (!btn || btn.classList.contains('added')) return;

        e.preventDefault();
        const carId = btn.dataset.carId;
        const carName = btn.dataset.carName;
        const carImage = btn.dataset.carImage;
        const price = btn.dataset.carPrice;

        btn.classList.add('adding');
        const originalHTML = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

        try {
            const token = getToken();
            const formData = new FormData();
            formData.append('carId', carId);
            formData.append('carName', carName);
            formData.append('carImage', carImage || '');
            formData.append('price', price);

            const response = await fetch('/Cart?handler=Add', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                // 1. Trigger flying car animation
                animateCarToCart(btn, carImage);

                // 2. After animation, update badge
                setTimeout(() => {
                    const badge = document.getElementById('cartBadge');
                    const cartIcon = badge?.closest('a')?.querySelector('.fa-shopping-cart');

                    if (badge) {
                        badge.textContent = result.cartCount;
                        badge.style.display = result.cartCount > 0 ? '' : 'none';
                        badge.classList.remove('cart-pulse');
                        void badge.offsetWidth;
                        badge.classList.add('cart-pulse');
                    }
                    if (cartIcon) {
                        cartIcon.classList.remove('cart-shake');
                        void cartIcon.offsetWidth;
                        cartIcon.classList.add('cart-shake');
                    }
                    if (badge) spawnParticles(badge);
                }, 700);

                // 3. Button success state
                setTimeout(() => {
                    btn.innerHTML = '<i class="fas fa-check me-1"></i>Đã thêm!';
                    btn.classList.add('added');
                    btn.classList.remove('adding');
                }, 600);

                // 4. Toast notification
                setTimeout(() => showCartToast(carName), 800);

                // 5. Reset button
                setTimeout(() => {
                    btn.innerHTML = originalHTML;
                    btn.classList.remove('added');
                }, 2500);
            } else {
                btn.innerHTML = originalHTML;
                btn.classList.remove('adding');
            }
        } catch (err) {
            console.error('Add to cart error:', err);
            btn.innerHTML = originalHTML;
            btn.classList.remove('adding');
            if (window.showToast) {
                showToast('Lỗi', 'Không thể thêm vào giỏ hàng. Vui lòng thử lại.', 'danger');
            }
        }
    });

    // ===== Flying Car Animation =====
    function animateCarToCart(btnElement, imageUrl) {
        const btnRect = btnElement.getBoundingClientRect();
        const cartBadge = document.getElementById('cartBadge');
        const cartLink = cartBadge?.closest('a');
        if (!cartLink) return;

        const cartRect = cartLink.getBoundingClientRect();

        const flyer = document.createElement('div');
        flyer.className = 'flying-car';

        if (imageUrl) {
            flyer.innerHTML = `<img src="${imageUrl}" alt="car" />`;
        } else {
            flyer.innerHTML = '<i class="fas fa-car-side car-icon-fallback"></i>';
        }

        document.body.appendChild(flyer);

        const startX = btnRect.left + btnRect.width / 2 - 40;
        const startY = btnRect.top + btnRect.height / 2 - 25;
        const endX = cartRect.left + cartRect.width / 2 - 40;
        const endY = cartRect.top + cartRect.height / 2 - 25;

        flyer.style.left = startX + 'px';
        flyer.style.top = startY + 'px';
        flyer.style.opacity = '1';
        flyer.style.transform = 'scale(1) rotate(0deg)';

        const duration = 700;
        const startTime = performance.now();

        function step(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            const ease = progress < 0.5
                ? 4 * progress * progress * progress
                : 1 - Math.pow(-2 * progress + 2, 3) / 2;

            const arcHeight = -120;
            const x = startX + (endX - startX) * ease;
            const y = startY + (endY - startY) * ease + arcHeight * Math.sin(Math.PI * progress);
            const scale = 1 - 0.6 * ease;
            const rotate = -15 * ease;

            flyer.style.left = x + 'px';
            flyer.style.top = y + 'px';
            flyer.style.transform = `scale(${scale}) rotate(${rotate}deg)`;
            flyer.style.opacity = `${1 - 0.3 * ease}`;

            if (progress < 1) {
                requestAnimationFrame(step);
            } else {
                flyer.style.opacity = '0';
                setTimeout(() => flyer.remove(), 100);
            }
        }

        requestAnimationFrame(step);
    }

    // ===== Particle Burst =====
    function spawnParticles(target) {
        const rect = target.getBoundingClientRect();
        const cx = rect.left + rect.width / 2;
        const cy = rect.top + rect.height / 2;
        const colors = ['#ff6b35', '#ffc107', '#198754', '#0dcaf0', '#fff'];

        for (let i = 0; i < 12; i++) {
            const p = document.createElement('div');
            p.className = 'cart-particle';
            p.style.left = cx + 'px';
            p.style.top = cy + 'px';
            p.style.background = colors[i % colors.length];
            document.body.appendChild(p);

            const angle = (Math.PI * 2 / 12) * i;
            const distance = 30 + Math.random() * 30;
            const dx = Math.cos(angle) * distance;
            const dy = Math.sin(angle) * distance;

            p.style.transition = 'all 0.5s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
            requestAnimationFrame(() => {
                p.style.left = (cx + dx) + 'px';
                p.style.top = (cy + dy) + 'px';
                p.style.opacity = '0';
                p.style.transform = 'scale(0)';
            });

            setTimeout(() => p.remove(), 600);
        }
    }

    // ===== Cart Toast =====
    function showCartToast(carName) {
        document.querySelectorAll('.cart-toast').forEach(t => t.remove());

        const toast = document.createElement('div');
        toast.className = 'cart-toast';
        toast.innerHTML = `
            <span class="toast-icon"><i class="fas fa-check-circle"></i></span>
            <div class="toast-body">
                <h6>Đã thêm vào giỏ!</h6>
                <p>${carName}</p>
            </div>
        `;

        document.body.appendChild(toast);
        requestAnimationFrame(() => toast.classList.add('show'));

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 400);
        }, 3000);
    }

    // ===== Quantity Buttons =====
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.qty-btn');
        if (!btn) return;

        const carId = btn.dataset.carId;
        const action = btn.dataset.action;
        const input = document.querySelector(`.qty-input[data-car-id="${carId}"]`);
        if (!input) return;

        let qty = parseInt(input.value) || 1;
        qty = action === 'increase' ? qty + 1 : Math.max(1, qty - 1);
        input.value = qty;

        await updateCartItem(carId, qty);
    });

    // ===== Remove from Cart =====
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.remove-cart-btn');
        if (!btn) return;

        const carId = btn.dataset.carId;
        const token = getToken();
        const formData = new FormData();
        formData.append('carId', carId);

        try {
            const response = await fetch('/Cart?handler=Remove', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });

            if (response.ok) location.reload();
        } catch (err) {
            console.error('Remove error:', err);
        }
    });

    async function updateCartItem(carId, quantity) {
        const token = getToken();
        const formData = new FormData();
        formData.append('carId', carId);
        formData.append('quantity', quantity);

        try {
            await fetch('/Cart?handler=Update', {
                method: 'POST',
                headers: { 'X-CSRF-TOKEN': token },
                body: formData
            });
            location.reload();
        } catch (err) {
            console.error('Update error:', err);
        }
    }
})();
