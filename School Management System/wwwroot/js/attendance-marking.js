(() => {
    const filters = document.getElementById('attendanceFilters');
    if (!filters) return;
    const form = document.getElementById('attendanceForm');
    const fields = [...(form?.querySelectorAll('.attendance-status') || [])];
    const rows = [...(form?.querySelectorAll('[data-attendance-row]') || [])];
    const save = form?.querySelector('button[type="submit"]');
    const state = document.getElementById('attendanceSaveState');
    let dirty = false, submitting = false, changedSelection = false, undo = null;
    const update = () => {
        const counts = fields.reduce((all, field) => { all[field.value] = (all[field.value] || 0) + 1; field.dataset.status = field.value; return all; }, {});
        const output = document.getElementById('attendanceProgress');
        if (output) output.textContent = `${fields.length} students · ${counts['1'] || 0} present · ${counts['2'] || 0} absent · ${counts['3'] || 0} late · ${counts['0'] || 0} unmarked`;
        const term = (document.getElementById('rosterSearch')?.value || '').trim().toLowerCase();
        const status = document.getElementById('rosterFilter')?.value;
        rows.forEach(row => row.hidden = !row.textContent.toLowerCase().includes(term) || (!!status && row.querySelector('.attendance-status').value !== status));
        const matches = document.getElementById('rosterMatches');
        if (matches) matches.textContent = `Showing ${rows.filter(row => !row.hidden).length} of ${rows.length} students. Save includes the whole class, including hidden rows.`;
    };
    const markDirty = () => { dirty = true; if (state) state.textContent = 'Unsaved changes — click Save attendance when finished.'; update(); };
    fields.forEach(field => field.addEventListener('change', () => { field.setCustomValidity(''); undo = null; const undoButton = document.getElementById('undoAttendanceBulk'); if (undoButton) undoButton.hidden = true; markDirty(); }));
    form?.querySelectorAll('input:not([type="hidden"]), textarea').forEach(input => input.addEventListener('input', markDirty));
    document.getElementById('rosterSearch')?.addEventListener('input', update);
    document.getElementById('rosterFilter')?.addEventListener('change', update);
    const bulk = (value, onlyUnmarked) => {
        const eligible = fields.filter(field => !field.disabled && (!onlyUnmarked || field.value === '0' || !field.value) && [...field.options].some(option => option.value === value));
        if (!eligible.length) return;
        undo = eligible.map(field => [field, field.value]);
        eligible.forEach(field => { field.value = value; field.setCustomValidity(''); });
        document.getElementById('undoAttendanceBulk').hidden = false;
        markDirty();
    };
    document.getElementById('markUnmarkedPresent')?.addEventListener('click', () => bulk('1', true));
    document.getElementById('markAllNoClass')?.addEventListener('click', () => bulk('7', false));
    document.getElementById('undoAttendanceBulk')?.addEventListener('click', event => {
        undo?.forEach(([field, value]) => field.value = value); undo = null; event.currentTarget.hidden = true; markDirty();
    });
    const sections = filters.querySelector('[name="sectionId"]');
    const classPicker = filters.querySelector('[name="schoolClassId"]');
    const load = filters.querySelector('button[type="submit"]');
    let request, generation = 0;
    const loadSections = async () => {
        request?.abort(); request = new AbortController(); const current = ++generation;
        if (!sections) return;
        sections.disabled = true; load.disabled = true;
        sections.replaceChildren(new Option('Loading sections…', ''));
        try {
            const response = await fetch(filters.dataset.sectionsUrl + '?' + new URLSearchParams(new FormData(filters)), { signal: request.signal });
            if (!response.ok) throw Error();
            const data = await response.json();
            if (current !== generation) return;
            sections.replaceChildren(new Option(data.length ? 'Choose a section' : 'No section', ''), ...data.map(section => new Option(section.name, section.id)));
            if (data.length === 1) sections.value = data[0].id;
            sections.required = data.length > 0;
        } catch (error) {
            if (current !== generation || error.name === 'AbortError') return;
            sections.required = false;
            sections.replaceChildren(new Option('Click Show students to refresh sections', ''));
        } finally { if (current === generation) { sections.disabled = false; load.disabled = false; } }
    };
    classPicker?.addEventListener('change', loadSections);
    filters.querySelector('[name="academicSessionId"]')?.addEventListener('change', loadSections);
    filters.querySelector('[name="attendanceDate"]')?.addEventListener('change', loadSections);
    filters.addEventListener('change', () => {
        changedSelection = true;
        if (save) save.disabled = true;
        const notice = document.getElementById('attendanceSelectionChanged'); if (notice) notice.hidden = false;
    });
    filters.addEventListener('submit', event => {
        if (dirty && !window.confirm('You have unsaved attendance changes. Discard them and load another class or date?')) { event.preventDefault(); return; }
        submitting = true;
    });
    form?.addEventListener('submit', event => {
        if (submitting || changedSelection) { event.preventDefault(); return; }
        const missing = fields.find(field => !field.value || field.value === '0');
        if (missing) {
            event.preventDefault(); document.getElementById('rosterSearch').value = ''; document.getElementById('rosterFilter').value = ''; update();
            missing.setCustomValidity('Choose attendance for this student before saving.'); missing.reportValidity(); missing.focus(); return;
        }
        submitting = true; save.disabled = true; save.textContent = 'Saving attendance…';
    });
    window.addEventListener('beforeunload', event => { if (dirty && !submitting) { event.preventDefault(); event.returnValue = ''; } });
    window.addEventListener('pageshow', () => { submitting = false; if (save) { save.disabled = changedSelection; save.textContent = 'Save attendance'; } });
    update();
})();
