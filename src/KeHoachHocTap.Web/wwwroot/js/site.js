/**
 * Hệ Thống Quản Lý Kế Hoạch Học Tập & Bảng Điểm
 * Classic Desktop Toast Dropdown System
 */

(function () {
    let lastToastMessage = null;
    let lastToastTime = 0;

    /**
     * Tạo hoặc lấy container chứa toast thả xuống
     */
    function getToastContainer() {
        let container = document.getElementById('toastDropdownContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastDropdownContainer';
            container.className = 'toast-dropdown-container';
            container.setAttribute('aria-live', 'polite');
            document.body.appendChild(container);
        }
        return container;
    }

    /**
     * Hiển thị toast thông báo thả xuống
     * @param {string} message - Nội dung thông báo
     * @param {'success'|'error'|'warning'|'info'} type - Loại thông báo
     * @param {string|null} title - Tiêu đề tùy chọn (bỏ qua nếu đơn giản)
     * @param {number} duration - Thời gian hiển thị (mili-giây, mặc định 2000ms)
     */
    window.showToast = function (message, type = 'success', title = null, duration = 2000) {
        if (!message || typeof message !== 'string') return;

        // Chống lặp thông báo giống nhau trong thời gian ngắn (800ms)
        const now = Date.now();
        if (lastToastMessage === message && (now - lastToastTime) < 800) {
            return;
        }
        lastToastMessage = message;
        lastToastTime = now;

        const container = getToastContainer();

        // Giới hạn tối đa 3 toast cùng lúc (xóa bớt toast cũ nhất nếu quá 3)
        while (container.children.length >= 3) {
            dismissToast(container.firstElementChild);
        }

        let iconSvg = '';
        switch (type) {
            case 'error':
            case 'danger':
                type = 'error';
                iconSvg = `<svg width="15" height="15" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM5.354 4.646a.5.5 0 1 0-.708.708L7.293 8l-2.647 2.646a.5.5 0 0 0 .708.708L8 8.707l2.646 2.647a.5.5 0 0 0 .708-.708L8.707 8l2.647-2.646a.5.5 0 0 0-.708-.708L8 7.293 5.354 4.646z"/>
                </svg>`;
                break;
            case 'warning':
                iconSvg = `<svg width="15" height="15" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
                </svg>`;
                break;
            case 'info':
                iconSvg = `<svg width="15" height="15" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/>
                </svg>`;
                break;
            default:
                type = 'success';
                iconSvg = `<svg width="15" height="15" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
                </svg>`;
                break;
        }

        // Tạo DOM element cho toast đơn giản, tinh gọn
        const toastEl = document.createElement('div');
        toastEl.className = `toast-dropdown-item toast-${type}`;
        toastEl.setAttribute('role', 'alert');

        toastEl.innerHTML = `
            <span class="toast-dropdown-icon">${iconSvg}</span>
            <span class="toast-dropdown-message">${escapeHtml(message)}</span>
            <button type="button" class="toast-dropdown-close" aria-label="Đóng">&times;</button>
        `;

        // Thêm vào container
        container.appendChild(toastEl);

        // Kích hoạt animation thả xuống mượt mà
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                toastEl.classList.add('toast-show');
            });
        });

        // Xử lý đếm ngược tự đóng và tạm dừng khi hover
        let remainingTime = duration;
        let startTime = Date.now();
        let dismissTimer = null;

        function startTimer() {
            startTime = Date.now();
            dismissTimer = setTimeout(() => {
                dismissToast(toastEl);
            }, remainingTime);
        }

        function pauseTimer() {
            clearTimeout(dismissTimer);
            remainingTime -= (Date.now() - startTime);
            if (remainingTime < 300) remainingTime = 300;
        }

        startTimer();

        toastEl.addEventListener('mouseenter', pauseTimer);
        toastEl.addEventListener('mouseleave', startTimer);

        // Đóng ngay khi bấm nút x hoặc bấm thẳng vào toast
        toastEl.addEventListener('click', () => {
            clearTimeout(dismissTimer);
            dismissToast(toastEl);
        });
    };

    /**
     * Thu hồi toast với hiệu ứng kéo lên
     */
    function dismissToast(toastEl) {
        if (!toastEl || toastEl.classList.contains('toast-hiding')) return;

        toastEl.classList.remove('toast-show');
        toastEl.classList.add('toast-hiding');

        setTimeout(() => {
            if (toastEl && toastEl.parentNode) {
                toastEl.parentNode.removeChild(toastEl);
            }
        }, 320);
    }

    /**
     * Escape an toàn chuỗi HTML
     */
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();

// =========================================================================
// Functional Buttons: Loading State & Anti-Double-Click Protection
// =========================================================================
(function () {
    /**
     * Xác định nhãn loading tương ứng theo văn bản gốc của nút
     */
    function getAppropriateLoadingText(originalText) {
        if (!originalText) return 'Đang xử lý...';
        const lower = originalText.toLowerCase().trim();
        if (lower.includes('xuất excel')) return 'Đang xuất Excel...';
        if (lower.includes('xuất word') || lower.includes('tải file word')) return 'Đang xuất Word...';
        if (lower.includes('xuất')) return 'Đang xuất file...';
        if (lower.includes('nạp lại') || lower.includes('nạp') || lower.includes('nhập')) return 'Đang nạp...';
        if (lower.includes('xóa')) return 'Đang xóa...';
        if (lower.includes('cập nhật')) return 'Đang cập nhật...';
        if (lower.includes('lưu')) return 'Đang lưu...';
        if (lower.includes('đăng nhập')) return 'Đang đăng nhập...';
        return 'Đang xử lý...';
    }

    /**
     * Áp dụng trạng thái loading cho button
     */
    function applyButtonLoading(btn, customText) {
        if (!btn || btn.classList.contains('btn-loading')) return;

        // Lưu trạng thái và kích thước hiện tại để tránh giật layout
        btn.dataset.originalHtml = btn.innerHTML;
        const currentWidth = btn.offsetWidth;
        if (currentWidth > 0) {
            btn.style.minWidth = currentWidth + 'px';
        }

        const text = customText || getAppropriateLoadingText(btn.textContent);
        btn.classList.add('btn-loading');
        btn.setAttribute('aria-busy', 'true');
        btn.innerHTML = `<span class="btn-loading-spinner" aria-hidden="true"></span><span>${text}</span>`;
    }

    /**
     * Phục hồi trạng thái ban đầu của button
     */
    function restoreButtonLoading(btn) {
        if (!btn || !btn.dataset.originalHtml) return;

        btn.innerHTML = btn.dataset.originalHtml;
        btn.classList.remove('btn-loading');
        btn.removeAttribute('aria-busy');
        btn.style.minWidth = '';
        delete btn.dataset.originalHtml;

        const form = btn.closest('form');
        if (form) {
            delete form.dataset.submitting;
        }
    }

    // Đăng ký sự kiện khi DOM sẵn sàng
    document.addEventListener('DOMContentLoaded', function () {
        // 1. Tự động bảo vệ tất cả Form submit: chống click đúp & hiển thị loading
        document.querySelectorAll('form').forEach(function (form) {
            form.addEventListener('submit', function (e) {
                // Nếu bị hủy bởi hàm kiểm tra inline (ví dụ confirm() trả về false)
                if (e.defaultPrevented) {
                    return;
                }

                // Nếu form không vượt qua HTML5 validation (ví dụ ô bắt buộc bị trống)
                if (form.checkValidity && !form.checkValidity()) {
                    return;
                }

                // Chống gửi lặp nếu form đang trong quá trình submit
                if (form.dataset.submitting === 'true') {
                    e.preventDefault();
                    return false;
                }

                // Tìm nút submit chính
                const submitBtn = form.querySelector('button[type="submit"]:not([data-no-loading]), input[type="submit"]:not([data-no-loading])');
                if (submitBtn) {
                    form.dataset.submitting = 'true';
                    applyButtonLoading(submitBtn);
                }
            });
        });

        // 2. Tự động gắn loading cho các nút Action Link tải file / xuất báo cáo (Excel, Word, v.v.)
        const exportSelector = 'a.btn-excel-export, a[href*="ExportExcel"], a[href*="handler=Export"], a[href*="handler=Download"], [data-loading]';
        document.querySelectorAll(exportSelector).forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                if (btn.classList.contains('btn-loading')) {
                    e.preventDefault();
                    return false;
                }

                applyButtonLoading(btn);

                // File download qua trình duyệt không gây unload trang,
                // do đó tự động hồi phục trạng thái nút sau 3.5 giây để sẵn sàng thao tác tiếp
                setTimeout(function () {
                    restoreButtonLoading(btn);
                }, 3500);
            });
        });
    });

    // Xuất hàm công khai để tái sử dụng ở bất kỳ component nào
    window.setButtonLoading = applyButtonLoading;
    window.resetButtonLoading = restoreButtonLoading;
})();

