// ===== Checkout JS =====
// Cascading Province → District → Ward dropdowns + 3-tier shipping + auto-calc

(function () {
    'use strict';

    const provinceSelect = document.getElementById('provinceSelect');
    const districtSelect = document.getElementById('districtSelect');
    const wardSelect = document.getElementById('wardSelect');
    if (!provinceSelect) return; // Not on checkout page

    const shippingTotalEl = document.getElementById('shippingTotal');
    const grandTotalEl = document.getElementById('grandTotal');
    const tierNameRow = document.getElementById('tierNameRow');
    const tierNameDisplay = document.getElementById('tierNameDisplay');
    const subtotalValue = parseFloat(document.getElementById('subtotalValue')?.value || '0');

    let currentShippingFee = 0;

    // ============================================================
    // HELPER: Get antiforgery token
    // ============================================================
    function getToken() {
        const el = document.getElementById('__AjaxAntiForgeryToken') || document.querySelector('input[name="__RequestVerificationToken"]');
        return el?.value || '';
    }

    // ============================================================
    // HELPER: Format VND currency
    // ============================================================
    function formatVND(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
    }

    // ============================================================
    // CASCADING DROPDOWNS
    // ============================================================

    // Province → load Districts
    provinceSelect.addEventListener('change', async function () {
        const provinceId = this.value;

        // Reset dependent dropdowns
        districtSelect.innerHTML = '<option value="">-- Đang tải... --</option>';
        districtSelect.disabled = true;
        wardSelect.innerHTML = '<option value="">-- Chọn Phường --</option>';
        wardSelect.disabled = true;
        resetShipping();

        if (!provinceId) {
            districtSelect.innerHTML = '<option value="">-- Chọn Quận --</option>';
            return;
        }

        try {
            const response = await fetch(`/Checkout?handler=Districts&provinceId=${provinceId}`);
            const districts = await response.json();

            districtSelect.innerHTML = '<option value="">-- Chọn Quận / Huyện --</option>';
            districts.forEach(d => {
                const opt = document.createElement('option');
                opt.value = d.districtId;
                opt.textContent = d.districtName;
                districtSelect.appendChild(opt);
            });
            districtSelect.disabled = false;
        } catch (err) {
            console.error('Load districts error:', err);
            districtSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
        }
    });

    // District → load Wards
    districtSelect.addEventListener('change', async function () {
        const districtId = this.value;

        wardSelect.innerHTML = '<option value="">-- Đang tải... --</option>';
        wardSelect.disabled = true;
        resetShipping();

        if (!districtId) {
            wardSelect.innerHTML = '<option value="">-- Chọn Phường --</option>';
            return;
        }

        try {
            const response = await fetch(`/Checkout?handler=Wards&districtId=${districtId}`);
            const wards = await response.json();

            wardSelect.innerHTML = '<option value="">-- Chọn Phường / Xã --</option>';
            wards.forEach(w => {
                const opt = document.createElement('option');
                opt.value = w.wardId;
                opt.textContent = w.wardName;
                wardSelect.appendChild(opt);
            });
            wardSelect.disabled = false;
        } catch (err) {
            console.error('Load wards error:', err);
            wardSelect.innerHTML = '<option value="">Không tải được dữ liệu</option>';
        }
    });

    // Ward selected → auto-calculate all tier fees
    wardSelect.addEventListener('change', function () {
        if (this.value) {
            calculateAllTiers();
        } else {
            resetShipping();
        }
    });

    // ============================================================
    // SHIPPING TIER SELECTION
    // ============================================================

    // Inject tier card styles
    const tierStyles = document.createElement('style');
    tierStyles.textContent = `
        .shipping-tier-card {
            display: block;
            cursor: pointer;
            transition: all 0.2s ease;
        }
        .shipping-tier-card .tier-border {
            border-color: rgba(255,255,255,0.15) !important;
            transition: all 0.25s ease;
            background: rgba(255,255,255,0.02);
        }
        .shipping-tier-card:hover .tier-border {
            border-color: rgba(255,107,53,0.4) !important;
            background: rgba(255,107,53,0.05);
        }
        .shipping-tier-card.active .tier-border {
            border-color: #ff6b35 !important;
            background: rgba(255,107,53,0.1);
            box-shadow: 0 0 12px rgba(255,107,53,0.15);
        }
        .shipping-tier-card .tier-icon {
            width: 44px;
            height: 44px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 10px;
            background: rgba(255,255,255,0.05);
        }
        .shipping-tier-card.active .tier-icon {
            background: rgba(255,107,53,0.15);
        }
        .tier-fee {
            font-size: 0.85rem;
            padding: 6px 12px;
        }
        .tier-fee.has-value {
            background: rgba(255,107,53,0.2) !important;
            color: #ff6b35 !important;
        }
    `;
    document.head.appendChild(tierStyles);

    // Tier card click handler
    document.querySelectorAll('.shipping-tier-card').forEach(card => {
        card.addEventListener('click', function () {
            // Remove active from all
            document.querySelectorAll('.shipping-tier-card').forEach(c => c.classList.remove('active'));
            this.classList.add('active');

            // Check the radio
            const radio = this.querySelector('.tier-radio');
            if (radio) radio.checked = true;

            // If we have fees calculated, update the displayed shipping
            updateSelectedTierFee();
        });
    });

    // ============================================================
    // SHIPPING FEE CALCULATION — auto for all 3 tiers
    // ============================================================

    const tierFees = { standard: 0, express: 0, hoatoc: 0 };

    async function calculateAllTiers() {
        const provinceId = provinceSelect.value;
        const districtId = districtSelect.value;
        if (!provinceId || !districtId) return;

        const token = getToken();
        const tiers = ['standard', 'express', 'hoatoc'];
        const feeEls = {
            standard: document.getElementById('feeStandard'),
            express: document.getElementById('feeExpress'),
            hoatoc: document.getElementById('feeHoatoc')
        };

        // Show loading
        for (const t of tiers) {
            if (feeEls[t]) feeEls[t].textContent = '⏳';
        }

        for (const tier of tiers) {
            try {
                const formData = new FormData();
                formData.append('provinceId', provinceId);
                formData.append('districtId', districtId);
                formData.append('shippingTier', tier);

                const response = await fetch('/Checkout?handler=CalculateShipping', {
                    method: 'POST',
                    headers: { 'X-CSRF-TOKEN': token },
                    body: formData
                });

                const result = await response.json();

                if (result.success) {
                    tierFees[tier] = result.fee;
                    if (feeEls[tier]) {
                        feeEls[tier].textContent = formatVND(result.fee);
                        feeEls[tier].classList.add('has-value');
                    }
                }
            } catch (err) {
                console.error(`Calc ${tier} error:`, err);
                if (feeEls[tier]) feeEls[tier].textContent = 'Lỗi';
            }
        }

        // Update selected tier fee in summary
        updateSelectedTierFee();
    }

    function updateSelectedTierFee() {
        const selectedRadio = document.querySelector('.tier-radio:checked');
        if (!selectedRadio) return;

        const tier = selectedRadio.value;
        const fee = tierFees[tier];

        if (fee > 0) {
            currentShippingFee = fee;
            if (shippingTotalEl) shippingTotalEl.textContent = formatVND(fee);
            if (grandTotalEl) grandTotalEl.textContent = formatVND(subtotalValue + fee);

            // Show tier name
            const tierNames = { standard: 'Tiêu chuẩn', express: 'Nhanh', hoatoc: 'Hỏa tốc' };
            const tierDays = { standard: '5-7 ngày', express: '2-3 ngày', hoatoc: '1-2 ngày' };
            if (tierNameRow) tierNameRow.style.display = '';
            if (tierNameRow) tierNameRow.style.setProperty('display', 'flex', 'important');
            if (tierNameDisplay) tierNameDisplay.textContent = `${tierNames[tier]} (${tierDays[tier]})`;
        }
    }

    function resetShipping() {
        tierFees.standard = 0;
        tierFees.express = 0;
        tierFees.hoatoc = 0;
        currentShippingFee = 0;

        document.querySelectorAll('.tier-fee').forEach(el => {
            el.textContent = '--';
            el.classList.remove('has-value');
        });

        if (shippingTotalEl) shippingTotalEl.textContent = 'Chọn địa chỉ...';
        if (grandTotalEl) grandTotalEl.textContent = formatVND(subtotalValue);
        if (tierNameRow) tierNameRow.style.setProperty('display', 'none', 'important');
    }
})();
