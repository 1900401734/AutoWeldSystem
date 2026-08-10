/*
 * 主题切换。刻意不走 Blazor JS interop：
 * - ServerPrerendered 下预渲染阶段无法调用 IJSRuntime，走原生 onclick 就完全没有这个问题
 * - 不经过 circuit 往返，点击即响应，circuit 未连接或重连中同样可用
 * - 服务端不持有主题状态，预渲染 HTML 不会与客户端存储的主题冲突
 * <html> 上的 data-theme 在组件渲染树之外，Blazor 差分不会触碰它。
 */
window.awsCenterTheme = {
   storageKey: 'aws-center-theme',

   toggle: function () {
      var next = document.documentElement.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
      document.documentElement.setAttribute('data-theme', next);
      try {
         localStorage.setItem(this.storageKey, next);
      } catch (e) {
         // 工控机可能以 kiosk 配置禁用 storage，此时仅本次会话生效。
      }
   }
};
