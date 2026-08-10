/*
 * 顶栏实时时钟。与 center-theme.js 同样刻意不走 Blazor JS interop：
 * - 每秒一次的 UI 更新若经 circuit 往返，会在工控机上产生持续的无谓流量与渲染
 * - circuit 断开/重连期间时钟照常走字，不会停在断开那一刻
 * 写入的是 .hero-clock-time 的 textContent，该节点由 Blazor 渲染但其文本
 * 不参与服务端差分（服务端只在快照刷新时才会重写它），因此不会互相打架。
 */
(function () {
   var SELECTOR = '.hero-clock-time';
   var timerId = null;

   function pad(value) {
      return value < 10 ? '0' + value : '' + value;
   }

   function tick() {
      var node = document.querySelector(SELECTOR);
      if (!node) {
         return;
      }
      var now = new Date();
      node.textContent = pad(now.getHours()) + ':' + pad(now.getMinutes()) + ':' + pad(now.getSeconds());
   }

   function start() {
      if (timerId !== null) {
         return;
      }
      tick();
      timerId = setInterval(tick, 1000);
   }

   // 标签页不可见时停表：工控机上看板常年开着，隐藏时没必要每秒唤醒渲染。
   document.addEventListener('visibilitychange', function () {
      if (document.hidden) {
         if (timerId !== null) {
            clearInterval(timerId);
            timerId = null;
         }
      } else {
         start();
      }
   });

   if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', start);
   } else {
      start();
   }
})();
