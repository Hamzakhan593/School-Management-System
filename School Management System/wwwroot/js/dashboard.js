document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.sms-panel, .sms-kpi-card').forEach((element, index) => {
        element.style.setProperty('--sms-delay', `${Math.min(index * 18, 180)}ms`);
    });
});
