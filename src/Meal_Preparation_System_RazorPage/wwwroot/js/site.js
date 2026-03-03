// ===========================
// Toast Notification System
// ===========================
const MealPrepToast = {
    container: null,

    init() {
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'toast-container-custom';
            document.body.appendChild(this.container);
        }
    },

    show(message, type = 'success') {
        this.init();

        const icons = {
            success: 'bi-check-circle-fill',
            error: 'bi-exclamation-triangle-fill',
            info: 'bi-info-circle-fill'
        };

        const toast = document.createElement('div');
        toast.className = `toast-custom toast-${type}`;
        toast.innerHTML = `
            <i class="bi ${icons[type] || icons.info} toast-icon"></i>
            <div class="toast-body">${message}</div>
            <button class="toast-close" aria-label="Close"><i class="bi bi-x-lg"></i></button>
            <div class="toast-progress"></div>
        `;

        this.container.appendChild(toast);

        const close = () => {
            toast.classList.add('toast-hiding');
            toast.addEventListener('animationend', () => toast.remove());
        };

        toast.querySelector('.toast-close').addEventListener('click', close);
        setTimeout(close, 4000);
    },

    success(message) { this.show(message, 'success'); },
    error(message) { this.show(message, 'error'); },
    info(message) { this.show(message, 'info'); }
};

// ===========================
// Custom Confirm Dialog
// ===========================
function showConfirm({ title = 'Are you sure?', message = 'This action cannot be undone.', icon = 'bi-trash', confirmText = 'Delete', cancelText = 'Cancel' } = {}) {
    return new Promise((resolve) => {
        const overlay = document.createElement('div');
        overlay.className = 'confirm-overlay';
        overlay.innerHTML = `
            <div class="confirm-dialog">
                <div class="confirm-icon" style="color: var(--clr-danger);"><i class="bi ${icon}"></i></div>
                <h5>${title}</h5>
                <p>${message}</p>
                <div class="confirm-actions">
                    <button class="btn-confirm-cancel">${cancelText}</button>
                    <button class="btn-confirm-ok">${confirmText}</button>
                </div>
            </div>
        `;

        document.body.appendChild(overlay);

        const cleanup = (result) => {
            overlay.style.animation = 'none';
            overlay.style.opacity = '0';
            overlay.style.transition = 'opacity .2s ease';
            setTimeout(() => { overlay.remove(); resolve(result); }, 200);
        };

        overlay.querySelector('.btn-confirm-cancel').addEventListener('click', () => cleanup(false));
        overlay.querySelector('.btn-confirm-ok').addEventListener('click', () => cleanup(true));
        overlay.addEventListener('click', (e) => { if (e.target === overlay) cleanup(false); });

        overlay.querySelector('.btn-confirm-ok').focus();
    });
}

// ===========================
// AI Loading Overlay
// ===========================
function showAILoading(message = 'Generating with AI') {
    const overlay = document.createElement('div');
    overlay.className = 'ai-loading-overlay';
    overlay.id = 'ai-loading-overlay';
    overlay.innerHTML = `
        <div class="ai-loading-card">
            <div class="ai-spinner"></div>
            <h5>${message}<span class="ai-dots"></span></h5>
            <p>This may take a moment. Please don't close the page.</p>
        </div>
    `;
    document.body.appendChild(overlay);
}

function hideAILoading() {
    const overlay = document.getElementById('ai-loading-overlay');
    if (overlay) overlay.remove();
}

// ===========================
// Auto-bind: data-confirm forms
// ===========================
document.addEventListener('DOMContentLoaded', () => {
    // Bind confirm dialogs to forms with data-confirm attribute
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const config = JSON.parse(form.dataset.confirm);
            const confirmed = await showConfirm(config);
            if (confirmed) form.submit();
        });
    });

    // Bind AI loading to forms with data-ai-loading attribute
    document.querySelectorAll('form[data-ai-loading]').forEach(form => {
        form.addEventListener('submit', () => {
            const message = form.dataset.aiLoading || 'Generating with AI';
            showAILoading(message);
        });
    });

    // Show TempData toasts
    const toastData = document.getElementById('toast-data');
    if (toastData) {
        const successMsg = toastData.dataset.success;
        const errorMsg = toastData.dataset.error;
        if (successMsg) MealPrepToast.success(successMsg);
        if (errorMsg) MealPrepToast.error(errorMsg);
    }

    // Show SignalR toast that was saved before a page reload
    const pendingToast = sessionStorage.getItem('pendingToast');
    if (pendingToast) {
        sessionStorage.removeItem('pendingToast');
        try {
            const { message, type } = JSON.parse(pendingToast);
            MealPrepToast.show(message, type);
        } catch (_) {}
    }
});
