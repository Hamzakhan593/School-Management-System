(() => {
    const root = document.querySelector('.classes-page');
    if (!root) return;
    function updateUrl(key, value) {
        const url = new URL(location.href);
        if (value) url.searchParams.set(key, value); else url.searchParams.delete(key);
        history.replaceState(null, '', url);
    }
    function showCreate(key, focus = true) {
        root.querySelectorAll('[data-create-panel]').forEach(panel => {
            panel.hidden = panel.dataset.createPanel !== key;
            if (!panel.hidden && focus) panel.querySelector('h3').focus({ preventScroll: true });
        });
        root.querySelectorAll('[data-create]').forEach(link => {
            if (link.dataset.create === key) link.setAttribute('aria-current', 'true'); else link.removeAttribute('aria-current');
        });
        const prompt = document.getElementById('createPrompt');
        if (prompt) prompt.hidden = !!key;
    }
    root.querySelectorAll('[data-create]').forEach(link => link.addEventListener('click', e => {
        e.preventDefault(); showCreate(link.dataset.create); updateUrl('create', link.dataset.create);
    }));
    root.querySelectorAll('[data-close-create]').forEach(link => link.addEventListener('click', e => {
        e.preventDefault(); showCreate(''); updateUrl('create', ''); root.querySelector('[data-create]')?.focus();
    }));
    root.querySelectorAll('[data-tab]').forEach(link => link.addEventListener('click', e => {
        e.preventDefault(); const tab = link.dataset.tab;
        root.querySelectorAll('[data-tab-panel]').forEach(panel => panel.hidden = panel.dataset.tabPanel !== tab);
        root.querySelectorAll('[data-tab]').forEach(item => { if (item === link) item.setAttribute('aria-current', 'page'); else item.removeAttribute('aria-current'); });
        root.querySelector('.classes-session input[name=tab]').value = tab;
        updateUrl('tab', tab);
    }));
    const search = document.getElementById('classSearch');
    search?.addEventListener('input', () => {
        let shown = 0;
        root.querySelectorAll('[data-class-card]').forEach(card => { card.hidden = !card.dataset.name.toLowerCase().includes(search.value.trim().toLowerCase()); if (!card.hidden) shown++; });
        document.getElementById('noClassMatches').hidden = shown > 0 || !search.value.trim();
    });
    const schoolClass = document.getElementById('academic-16');
    const section = document.getElementById('academic-17');
    function refreshSections() {
        if (!schoolClass || !section) return;
        [...section.options].forEach(option => { option.hidden = option.disabled = !!option.value && option.dataset.class !== schoolClass.value; });
        if (section.selectedOptions[0]?.disabled) section.value = '';
        const subject = document.getElementById('academic-18');
        if (subject) {
            [...subject.options].forEach(option => { option.hidden = option.disabled = !!option.value && !(option.dataset.classes || '').split(',').includes(schoolClass.value); });
            if (subject.selectedOptions[0]?.disabled) subject.value = '';
        }
    }
    schoolClass?.addEventListener('change', refreshSections);
    refreshSections();
})();
