(function () {
    let reference;
    let rotationTimer;
    let rotationButton;
    let reconnectTimer;
    window.awsCenterDisplay = {
        attach: function (element, callback) { reference = callback; },
        detach: function () { reference = null; clearInterval(rotationTimer); },
        fullscreen: async function () {
            try {
                if (document.fullscreenElement) await document.exitFullscreen();
                else await document.documentElement.requestFullscreen();
            } catch (_) { /* 浏览器限制全屏时仍可正常使用窗口。 */ }
        },
        toggleMotion: function (button) {
            const off = document.documentElement.dataset.motion !== 'off';
            document.documentElement.dataset.motion = off ? 'off' : 'on';
            button.setAttribute('aria-pressed', String(off));
            button.textContent = off ? '恢复动画' : '减少动画';
        },
        toggleRotation: async function (button) {
            rotationButton = button;
            if (rotationTimer) { stopRotation(); return; }
            if (!document.fullscreenElement) await this.fullscreen();
            if (!document.fullscreenElement) return;
            button.textContent = '暂停轮播';
            button.setAttribute('aria-pressed', 'true');
            rotationTimer = setInterval(() => {
                if (!document.hidden && reference) reference.invokeMethodAsync('RotatePage').catch(() => {});
            }, 15000);
        }
    };
    function stopRotation() {
        clearInterval(rotationTimer);
        rotationTimer = null;
        if (rotationButton) {
            rotationButton.textContent = '全屏轮播';
            rotationButton.setAttribute('aria-pressed', 'false');
        }
    }
    document.addEventListener('fullscreenchange', () => { if (!document.fullscreenElement) stopRotation(); });

    // 服务重启后旧 circuit 无法恢复；等本机健康检查通过后再重载，避免持续闪页。
    function watchReconnect() {
        const modal = document.getElementById('components-reconnect-modal');
        if (!modal) return;
        const observer = new MutationObserver(() => {
            clearTimeout(reconnectTimer);
            if (modal.classList.contains('components-reconnect-rejected') || modal.classList.contains('components-reconnect-failed')) {
                reconnectTimer = setTimeout(async function retry() {
                    try {
                        const response = await fetch('healthz', { cache: 'no-store' });
                        if (response.ok) { location.reload(); return; }
                    } catch (_) { }
                    reconnectTimer = setTimeout(retry, 10000);
                }, 10000);
            }
        });
        observer.observe(modal, { attributes: true, attributeFilter: ['class'] });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', watchReconnect);
    else watchReconnect();
})();
