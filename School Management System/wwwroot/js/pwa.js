(() => {
  'use strict';

  let deferredInstallPrompt = null;
  let serviceWorkerReady = false;

  const qsAll = selector => Array.from(document.querySelectorAll(selector));
  const isStandalone = () => window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
  const isIos = () => /iphone|ipad|ipod/i.test(navigator.userAgent);

  function setText(selector, text) {
    qsAll(selector).forEach(el => { el.textContent = text; });
  }

  function setInstallVisibility(show) {
    qsAll('[data-pwa-install]').forEach(el => el.classList.toggle('d-none', !show));
  }

  function updateConnectionUi() {
    const online = navigator.onLine;
    document.body.classList.toggle('pwa-offline', !online);
    const banner = document.querySelector('[data-pwa-offline-banner]');
    if (banner) banner.hidden = online;
    qsAll('[data-pwa-connection]').forEach(el => {
      el.classList.toggle('offline', !online);
      const strong = el.querySelector('strong');
      if (strong) strong.textContent = online ? 'Online' : 'Offline';
    });
    setText('[data-pwa-diag="connection"]', online ? 'Online' : 'Offline');
    updateDiagnostics();
  }

  function updateDiagnostics() {
    const secure = window.isSecureContext;
    const standalone = isStandalone();
    const camera = !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
    setText('[data-pwa-diag="secure"]', secure ? 'Available' : 'HTTPS required');
    setText('[data-pwa-diag="worker"]', serviceWorkerReady ? 'Active' : ('serviceWorker' in navigator ? 'Registering…' : 'Not supported'));
    setText('[data-pwa-diag="standalone"]', standalone ? 'Installed' : 'Browser mode');
    setText('[data-pwa-diag="camera"]', camera ? 'Supported' : 'Not supported');

    const overall = document.querySelector('[data-pwa-diag="overall"]');
    if (overall) {
      const ready = secure && serviceWorkerReady;
      overall.textContent = ready ? 'PWA ready' : 'Setup check';
      overall.classList.toggle('success', ready);
    }
  }

  function showManualInstallHelp() {
    const message = isIos()
      ? 'On iPhone/iPad: open this site in Safari, tap Share, choose “Add to Home Screen”, then tap Add.'
      : 'If the browser install prompt is not available yet, open the browser menu and choose “Install app” or “Add to Home screen”. The production site must use HTTPS.';
    window.alert(message);
  }

  async function requestInstall() {
    if (isStandalone()) return;
    if (!deferredInstallPrompt) {
      showManualInstallHelp();
      return;
    }
    deferredInstallPrompt.prompt();
    try { await deferredInstallPrompt.userChoice; } catch (_) { }
    deferredInstallPrompt = null;
    setInstallVisibility(false);
    updateDiagnostics();
  }

  window.addEventListener('beforeinstallprompt', event => {
    event.preventDefault();
    deferredInstallPrompt = event;
    if (!isStandalone()) setInstallVisibility(true);
  });

  window.addEventListener('appinstalled', () => {
    deferredInstallPrompt = null;
    setInstallVisibility(false);
    updateDiagnostics();
  });

  window.addEventListener('online', updateConnectionUi);
  window.addEventListener('offline', updateConnectionUi);

  document.addEventListener('click', event => {
    const install = event.target.closest('[data-pwa-install], [data-pwa-install-manual]');
    if (install) {
      event.preventDefault();
      requestInstall();
    }
  });

  // Never pretend that write operations succeed offline. No POST/PUT/DELETE queue is used.
  document.addEventListener('submit', event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) return;
    const method = (form.getAttribute('method') || 'get').toLowerCase();
    if (method !== 'get' && !navigator.onLine) {
      event.preventDefault();
      window.alert('You are offline. This change was NOT submitted or queued. Reconnect to the school server and try again.');
    }
  });

  if ('serviceWorker' in navigator) {
    window.addEventListener('load', async () => {
      try {
        const registration = await navigator.serviceWorker.register('/service-worker.js', { scope: '/' });
        await navigator.serviceWorker.ready;
        serviceWorkerReady = true;
        updateDiagnostics();
        if (registration.waiting) registration.waiting.postMessage('SKIP_WAITING');
      } catch (error) {
        console.warn('PWA service worker registration failed.', error);
        serviceWorkerReady = false;
        updateDiagnostics();
      }
    });
  }

  setInstallVisibility(false);
  updateConnectionUi();
  updateDiagnostics();
})();
