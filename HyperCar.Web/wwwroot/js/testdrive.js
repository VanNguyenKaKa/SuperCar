/**
 * testdrive.js — Test Drive Booking Module
 * Fixed: FormData + antiforgery in body, dark buttons, date picker, SignalR
 */
(function () {
    'use strict';

    // ── DOM refs ──
    const dateInput = document.getElementById('testDriveDate');
    const slotsContainer = document.getElementById('timeSlotsContainer');
    const timerEl = document.getElementById('bookingTimer');
    const timerWrap = document.getElementById('timerWrap');
    const submitBtn = document.getElementById('submitTestDriveBtn');
    const notesInput = document.getElementById('testDriveNotes');
    const showroomSelect = document.getElementById('testDriveShowroom');
    const policyCheck = document.getElementById('policyAgree');
    const resultMsg = document.getElementById('testDriveResult');
    const modal = document.getElementById('testDriveModal');

    if (!dateInput || !slotsContainer || !modal) return;

    const carId = parseInt(modal.dataset.carId) || 0;
    if (!carId) return;

    // ── State ──
    let selectedSlot = null;
    let countdownInterval = null;
    let holdSeconds = 300;

    // ── Antiforgery token ──
    function getToken() {
        const el = document.getElementById('__AjaxAntiForgeryToken');
        return el ? el.value : '';
    }
    function getTokenName() {
        const el = document.getElementById('__AjaxAntiForgeryToken');
        return el ? el.name : '__RequestVerificationToken';
    }

    // Build FormData with antiforgery token baked in
    function buildFormData(fields) {
        const fd = new FormData();
        fd.append(getTokenName(), getToken());
        for (const [key, val] of Object.entries(fields)) {
            if (val !== null && val !== undefined && val !== '') {
                fd.append(key, val);
            }
        }
        return fd;
    }

    // ══════════════════════════════════════════════════
    // FETCH AVAILABLE SLOTS (GET — no antiforgery needed)
    // ══════════════════════════════════════════════════
    async function fetchSlots() {
        const date = dateInput.value;
        if (!date) { slotsContainer.innerHTML = ''; return; }

        slotsContainer.innerHTML = '<div class="text-center py-3"><i class="fas fa-spinner fa-spin me-2"></i>Đang tải...</div>';

        try {
            const res = await fetch(`?handler=AvailableSlots&carId=${carId}&date=${date}`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();

            if (!data.success || !data.slots?.length) {
                slotsContainer.innerHTML = '<p class="text-muted text-center">Không có khung giờ khả dụng.</p>';
                return;
            }

            let html = '<div class="d-flex flex-wrap gap-2 justify-content-center">';
            data.slots.forEach(slot => {
                const dt = new Date(slot.slotTime);
                const hh = String(dt.getHours()).padStart(2, '0');
                const mm = String(dt.getMinutes()).padStart(2, '0');
                const display = `${hh}:${mm}`;
                const isoVal = slot.slotTime;

                if (slot.status === 'booked') {
                    html += `<button type="button" class="btn btn-sm btn-secondary btn-slot disabled" disabled data-slot="${isoVal}">
                        <i class="fas fa-lock me-1"></i>${display}
                    </button>`;
                } else if (slot.status === 'held') {
                    html += `<button type="button" class="btn btn-sm btn-warning btn-slot disabled" disabled data-slot="${isoVal}">
                        <i class="fas fa-hourglass-half me-1"></i>${display}
                    </button>`;
                } else {
                    html += `<button type="button" class="btn btn-sm btn-outline-dark btn-slot" data-slot="${isoVal}" style="min-width:70px;font-weight:600;">
                        ${display}
                    </button>`;
                }
            });
            html += '</div>';
            slotsContainer.innerHTML = html;

            slotsContainer.querySelectorAll('.btn-slot:not(.disabled)').forEach(btn => {
                btn.addEventListener('click', () => holdSlot(btn));
            });

        } catch (err) {
            console.error('Fetch slots error:', err);
            slotsContainer.innerHTML = '<p class="text-danger text-center">Lỗi tải khung giờ.</p>';
        }
    }

    // ══════════════════════════════════════════════════
    // HOLD SLOT (POST with FormData + antiforgery in body)
    // ══════════════════════════════════════════════════
    async function holdSlot(btn) {
        const slotValue = btn.dataset.slot;

        clearCountdown();
        slotsContainer.querySelectorAll('.btn-slot').forEach(b => {
            b.classList.remove('active');
            b.style.background = '';
            b.style.color = '';
            b.style.borderColor = '';
        });

        // Visual selection
        btn.style.background = '#c0a23d';
        btn.style.color = '#fff';
        btn.style.borderColor = '#c0a23d';
        btn.classList.add('active');
        const origText = btn.textContent.trim();
        btn.innerHTML = `<i class="fas fa-spinner fa-spin me-1"></i>${origText}`;

        try {
            const res = await fetch(`?handler=HoldSlot&carId=${carId}`, {
                method: 'POST',
                body: buildFormData({ scheduledDate: slotValue })
            });

            if (!res.ok) {
                const text = await res.text();
                console.error('HoldSlot response:', res.status, text);
                throw new Error(`HTTP ${res.status}`);
            }

            const data = await res.json();

            if (data.success) {
                selectedSlot = slotValue;
                const dt = new Date(slotValue);
                const display = `${String(dt.getHours()).padStart(2, '0')}:${String(dt.getMinutes()).padStart(2, '0')}`;
                btn.innerHTML = `<i class="fas fa-check me-1"></i>${display}`;
                checkCanSubmit();
                startCountdown();
                if (resultMsg) { resultMsg.classList.add('d-none'); resultMsg.textContent = ''; }
            } else {
                resetBtnStyle(btn, origText);
                showResult(data.error || 'Không thể giữ khung giờ.', 'danger');
                fetchSlots();
            }
        } catch (err) {
            console.error('Hold slot error:', err);
            resetBtnStyle(btn, origText);
            showResult('Lỗi mạng. Vui lòng thử lại.', 'danger');
        }
    }

    function resetBtnStyle(btn, text) {
        btn.classList.remove('active');
        btn.style.background = '';
        btn.style.color = '';
        btn.style.borderColor = '';
        btn.innerHTML = text;
    }

    // ══════════════════════════════════════════════════
    // COUNTDOWN TIMER (5 min)
    // ══════════════════════════════════════════════════
    function startCountdown() {
        holdSeconds = 300;
        if (timerWrap) timerWrap.classList.remove('d-none');
        updateTimerDisplay();

        countdownInterval = setInterval(() => {
            holdSeconds--;
            updateTimerDisplay();
            if (holdSeconds <= 0) {
                clearCountdown();
                selectedSlot = null;
                checkCanSubmit();
                showResult('⏰ Khung giờ hết hạn. Vui lòng chọn lại.', 'warning');
                fetchSlots();
            }
        }, 1000);
    }

    function updateTimerDisplay() {
        if (!timerEl) return;
        const min = Math.floor(holdSeconds / 60);
        const sec = holdSeconds % 60;
        timerEl.textContent = `${String(min).padStart(2, '0')}:${String(sec).padStart(2, '0')}`;
        timerEl.style.color = holdSeconds <= 60 ? '#dc3545' : '#c0a23d';
    }

    function clearCountdown() {
        if (countdownInterval) clearInterval(countdownInterval);
        countdownInterval = null;
        if (timerWrap) timerWrap.classList.add('d-none');
    }

    // ══════════════════════════════════════════════════
    // CHECK SUBMIT ELIGIBILITY
    // ══════════════════════════════════════════════════
    function checkCanSubmit() {
        if (!submitBtn) return;
        submitBtn.disabled = !(selectedSlot && policyCheck && policyCheck.checked);
    }

    // ══════════════════════════════════════════════════
    // SUBMIT BOOKING
    // ══════════════════════════════════════════════════
    async function submitBooking() {
        if (!selectedSlot) { showResult('Vui lòng chọn khung giờ.', 'warning'); return; }
        if (policyCheck && !policyCheck.checked) { showResult('Vui lòng đồng ý điều khoản.', 'warning'); return; }

        submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang xử lý...';

        try {
            const res = await fetch(`?handler=SubmitTestDrive&carId=${carId}`, {
                method: 'POST',
                body: buildFormData({
                    scheduledDate: selectedSlot,
                    notes: notesInput?.value || '',
                    showroomId: showroomSelect?.value || ''
                })
            });

            if (!res.ok) {
                const text = await res.text();
                console.error('SubmitTestDrive response:', res.status, text);
                throw new Error(`HTTP ${res.status}`);
            }

            const data = await res.json();

            if (data.success) {
                clearCountdown();
                showResult('✅ Đặt lịch lái thử thành công! Chúng tôi sẽ xác nhận sớm nhất.', 'success');
                selectedSlot = null;
                submitBtn.innerHTML = '<i class="fas fa-check me-2"></i>Đã đặt!';
                setTimeout(() => {
                    fetchSlots();
                    submitBtn.innerHTML = '<i class="fas fa-paper-plane me-2"></i>Xác nhận đặt lịch';
                    submitBtn.disabled = true;
                }, 2000);
            } else {
                showResult(data.error || 'Có lỗi xảy ra.', 'danger');
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="fas fa-paper-plane me-2"></i>Xác nhận đặt lịch';
            }
        } catch (err) {
            console.error('Submit error:', err);
            showResult('Lỗi mạng. Vui lòng thử lại.', 'danger');
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="fas fa-paper-plane me-2"></i>Xác nhận đặt lịch';
        }
    }

    // ══════════════════════════════════════════════════
    // SIGNALR
    // ══════════════════════════════════════════════════
    function initSignalR() {
        const conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notification')
            .withAutomaticReconnect()
            .build();

        conn.on('ReceiveSlotLocked', (lockedCarId, timeSlot) => {
            if (lockedCarId !== carId) return;
            const btn = slotsContainer.querySelector(`[data-slot="${timeSlot}"]`);
            if (btn && !btn.classList.contains('active')) {
                btn.classList.add('disabled');
                btn.disabled = true;
                btn.className = 'btn btn-sm btn-warning btn-slot disabled';
                const dt = new Date(timeSlot);
                btn.innerHTML = `<i class="fas fa-hourglass-half me-1"></i>${String(dt.getHours()).padStart(2,'0')}:${String(dt.getMinutes()).padStart(2,'0')}`;
            }
        });

        conn.on('ReceiveSlotBooked', (bookedCarId, timeSlot) => {
            if (bookedCarId !== carId) return;
            const btn = slotsContainer.querySelector(`[data-slot="${timeSlot}"]`);
            if (btn && !btn.classList.contains('active')) {
                btn.classList.add('disabled');
                btn.disabled = true;
                btn.className = 'btn btn-sm btn-secondary btn-slot disabled';
                const dt = new Date(timeSlot);
                btn.innerHTML = `<i class="fas fa-lock me-1"></i>${String(dt.getHours()).padStart(2,'0')}:${String(dt.getMinutes()).padStart(2,'0')}`;
            }
        });

        conn.on('ReceiveSlotReleased', (releasedCarId) => {
            if (releasedCarId !== carId) return;
            fetchSlots();
        });

        conn.start()
            .then(() => console.log('TestDrive SignalR connected'))
            .catch(err => console.log('TestDrive SignalR error:', err));
    }

    // ── Helpers ──
    function showResult(msg, type) {
        if (!resultMsg) return;
        resultMsg.textContent = msg;
        resultMsg.className = `alert alert-${type} mt-3 mb-0`;
        resultMsg.classList.remove('d-none');
    }

    // ── Event bindings ──
    dateInput.addEventListener('change', () => {
        clearCountdown();
        selectedSlot = null;
        checkCanSubmit();
        fetchSlots();
    });

    if (submitBtn) submitBtn.addEventListener('click', submitBooking);
    if (policyCheck) policyCheck.addEventListener('change', checkCanSubmit);

    modal.addEventListener('hidden.bs.modal', () => {
        clearCountdown();
        selectedSlot = null;
        checkCanSubmit();
    });

    // ── Init ──
    initSignalR();
})();
