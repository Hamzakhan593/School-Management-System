(() => {
    const form = document.getElementById('marksForm');
    if (!form) return;
    const rows = [...form.querySelectorAll('[data-mark-row]')];
    const search = document.getElementById('marksSearch');
    const filter = document.getElementById('marksFilter');
    let dirty = false;
    function complete(row) {
        const status = row.querySelector('.mark-status');
        if (!status) return true;
        return status.value !== '0' || [...row.querySelectorAll('.mark-input')].filter(i => !i.disabled).every(i => i.value !== '' && i.validity.valid);
    }
    function update() {
        const needle = search.value.trim().toLowerCase();
        let count = 0;
        rows.forEach(row => {
            const done = complete(row);
            if (done) count++;
            row.classList.toggle('mark-incomplete', !done);
            row.hidden = !row.textContent.toLowerCase().includes(needle) || (filter.value === 'complete' && !done) || (filter.value === 'incomplete' && done);
        });
        document.getElementById('marksProgress').textContent = `${count} of ${rows.length} complete · ${rows.length - count} remaining`;
    }
    form.querySelectorAll('.mark-status').forEach(select => {
        function toggle() { select.closest('tr').querySelectorAll('.mark-input').forEach(i => { i.disabled = select.value !== '0'; }); }
        toggle();
        select.addEventListener('change', toggle);
    });
    form.addEventListener('input', e => {
        if (e.target === search || e.target === filter) return;
        dirty = true;
        const note = form.querySelector('[data-dirty]');
        if (note) note.textContent = 'Unsaved changes — save your draft before leaving.';
        update();
    });
    form.addEventListener('change', update);
    search.addEventListener('input', update);
    filter.addEventListener('change', update);
    form.addEventListener('keydown', e => {
        if (e.key !== 'Enter' || !e.target.matches('.mark-input')) return;
        e.preventDefault();
        const inputs = rows.filter(r => !r.hidden).flatMap(r => [...r.querySelectorAll('.mark-input')]).filter(i => !i.disabled);
        const next = inputs[inputs.indexOf(e.target) + 1];
        if (next) { next.focus(); next.select(); }
    });
    form.addEventListener('submit', e => {
        if (e.submitter?.value === 'true' && rows.some(r => !complete(r))) {
            e.preventDefault(); filter.value = 'incomplete'; search.value = ''; update();
            rows.find(r => !complete(r))?.querySelector('.mark-input:not(:disabled)')?.focus();
            form.querySelector('[data-dirty]').textContent = 'Complete the highlighted students, or use Save draft to finish later.';
            return;
        }
        dirty = false;
    });
    window.addEventListener('beforeunload', e => { if (dirty) { e.preventDefault(); e.returnValue = ''; } });
    update();
})();
