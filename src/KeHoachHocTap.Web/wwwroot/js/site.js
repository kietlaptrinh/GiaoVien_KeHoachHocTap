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
     * @param {string|null} title - Tiêu đề tùy chọn
     * @param {number} duration - Thời gian hiển thị (mili-giây, mặc định 4000ms)
     */
    window.showToast = function (message, type = 'success', title = null, duration = 4000) {
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

        // Cấu hình tiêu đề và icon theo loại
        let defaultTitle = '✓ Thành công';
        let iconSvg = '';

        switch (type) {
            case 'error':
            case 'danger':
                type = 'error';
                defaultTitle = '✕ Có lỗi xảy ra';
                iconSvg = `<svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zM5.354 4.646a.5.5 0 1 0-.708.708L7.293 8l-2.647 2.646a.5.5 0 0 0 .708.708L8 8.707l2.646 2.647a.5.5 0 0 0 .708-.708L8.707 8l2.647-2.646a.5.5 0 0 0-.708-.708L8 7.293 5.354 4.646z"/>
                </svg>`;
                break;
            case 'warning':
                defaultTitle = '⚠️ Cảnh báo';
                iconSvg = `<svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
                </svg>`;
                break;
            case 'info':
                defaultTitle = 'ℹ Thông báo';
                iconSvg = `<svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/>
                </svg>`;
                break;
            default:
                type = 'success';
                defaultTitle = '✓ Thành công';
                iconSvg = `<svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M16 8A8 8 0 1 1 0 8a8 8 0 0 1 16 0zm-3.97-3.03a.75.75 0 0 0-1.08.022L7.477 9.417 5.384 7.323a.75.75 0 0 0-1.06 1.06L6.97 11.03a.75.75 0 0 0 1.079-.02l3.992-4.99a.75.75 0 0 0-.01-1.05z"/>
                </svg>`;
                break;
        }

        const displayTitle = title || defaultTitle;

        // Tạo DOM element cho toast
        const toastEl = document.createElement('div');
        toastEl.className = `toast-dropdown-item toast-${type}`;
        toastEl.setAttribute('role', 'alert');

        toastEl.innerHTML = `
            <div class="toast-dropdown-icon">${iconSvg}</div>
            <div class="toast-dropdown-content">
                <div class="toast-dropdown-title">${escapeHtml(displayTitle)}</div>
                <div class="toast-dropdown-message">${escapeHtml(message)}</div>
            </div>
            <button type="button" class="toast-dropdown-close" aria-label="Đóng">&times;</button>
            <div class="toast-dropdown-progress">
                <div class="toast-dropdown-progress-bar" style="transition-duration: ${duration}ms;"></div>
            </div>
        `;

        // Thêm vào container
        container.appendChild(toastEl);

        // Kích hoạt animation thả xuống mượt mà
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                toastEl.classList.add('toast-show');
                const progressBar = toastEl.querySelector('.toast-dropdown-progress-bar');
                if (progressBar) {
                    progressBar.style.transform = 'scaleX(0)';
                }
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
            if (remainingTime < 500) remainingTime = 500;
        }

        startTimer();

        toastEl.addEventListener('mouseenter', () => {
            pauseTimer();
            const progressBar = toastEl.querySelector('.toast-dropdown-progress-bar');
            if (progressBar) progressBar.style.transitionPlayState = 'paused';
        });

        toastEl.addEventListener('mouseleave', () => {
            startTimer();
            const progressBar = toastEl.querySelector('.toast-dropdown-progress-bar');
            if (progressBar) progressBar.style.transitionPlayState = 'running';
        });

        // Nút đóng thủ công
        const closeBtn = toastEl.querySelector('.toast-dropdown-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                clearTimeout(dismissTimer);
                dismissToast(toastEl);
            });
        }
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
