(() => {
    const form = document.getElementById('generationForm');
    if (!form) return;
    const commit = document.getElementById('challanCommitForm');
    const classPicker = form.querySelector('#SchoolClassId');
    const sectionPicker = form.querySelector('#SectionId');
    const studentPicker = form.querySelector('#StudentId');
    let stale = false;
    const invalidate = () => {
        if (!commit) return;
        stale = true;
        commit.querySelector('button').disabled = true;
        document.getElementById('previewChanged').hidden = false;
        document.getElementById('readyPdf').hidden = true;
    };
    const updateScope = () => {
        const scope = form.querySelector('[name="Scope"]:checked')?.value;
        form.querySelectorAll('[data-scope]').forEach(panel => {
            const active = panel.dataset.scope === scope;
            panel.hidden = !active;
            panel.querySelectorAll('input, select, button').forEach(input => input.disabled = !active);
        });
        studentPicker.required = scope === 'Individual';
        classPicker.required = scope === 'ClassSection';
        updateSections();
    };
    const updateSections = () => {
        [...sectionPicker.options].forEach(option => {
            const valid = !option.value || option.dataset.class === classPicker.value;
            option.hidden = !valid;
            option.disabled = !valid;
        });
        if (sectionPicker.selectedOptions[0]?.disabled) sectionPicker.value = '';
        sectionPicker.disabled = classPicker.disabled || !classPicker.value;
    };
    form.querySelectorAll('[name="Scope"]').forEach(input => input.addEventListener('change', updateScope));
    classPicker.addEventListener('change', updateSections);
    form.addEventListener('change', event => { if (event.target.type !== 'search') invalidate(); });
    document.getElementById('billingMonthPicker').addEventListener('input', invalidate);
    document.getElementById('studentSearch').addEventListener('input', event => {
        const term = event.target.value.trim().toLowerCase();
        [...studentPicker.options].forEach(option => {
            option.hidden = !!option.value && !option.text.toLowerCase().includes(term);
        });
    });
    const checks = [...form.querySelectorAll('[name="SelectedStudentIds"]')];
    const updateCount = () => document.getElementById('selectionCount').textContent = `${checks.filter(input => input.checked).length} student(s) selected`;
    checks.forEach(input => input.addEventListener('change', updateCount));
    document.getElementById('clearStudents').addEventListener('click', () => {
        checks.forEach(input => input.checked = false);
        updateCount(); invalidate();
    });
    document.getElementById('multiSearch').addEventListener('input', event => {
        const term = event.target.value.trim().toLowerCase();
        checks.forEach(input => input.closest('label').hidden = !input.closest('label').textContent.toLowerCase().includes(term));
        document.getElementById('noStudentMatches').hidden = checks.some(input => !input.closest('label').hidden);
    });
    form.addEventListener('submit', event => {
        if (form.querySelector('[name="Scope"]:checked')?.value === 'SelectedStudents' && !checks.some(input => input.checked)) {
            event.preventDefault();
            document.getElementById('selectionCount').textContent = 'Tick at least one student to continue.';
            document.getElementById('multiSearch').focus();
            return;
        }
        document.getElementById('billingMonthValue').value = document.getElementById('billingMonthPicker').value + '-01';
        const button = form.querySelector('button[type="submit"]');
        button.disabled = true; button.textContent = 'Checking fees…';
    });
    updateScope(); updateCount();
    window.addEventListener('pageshow', () => {
        const button = form.querySelector('button[type="submit"]');
        if (!commit?.dataset.downloading) {
            button.disabled = false;
            button.textContent = 'Check challans →';
        }
    });
    document.getElementById('previewTitle')?.focus({ preventScroll: true });
    document.getElementById('challanPreview')?.scrollIntoView({ block: 'start' });
    let pdfUrl;
    commit?.addEventListener('submit', async event => {
        event.preventDefault();
        if (stale || commit.dataset.downloading) return;
        const button = commit.querySelector('button');
        const originalText = button.textContent;
        const status = document.getElementById('downloadStatus');
        const ready = document.getElementById('readyPdf');
        const data = new FormData(commit); data.set('download', 'true');
        commit.dataset.downloading = 'true'; button.disabled = true;
        button.textContent = 'Preparing PDF…';
        status.textContent = 'Saving challans and preparing your PDF. Please keep this page open.';
        // Keep the visible selection aligned with the request while it is saving.
        const enabled = [...form.elements].filter(input => !input.disabled);
        enabled.forEach(input => input.disabled = true);
        try {
            const response = await fetch(commit.action, { method: 'POST', body: data });
            if (!response.ok || !response.headers.get('content-type')?.includes('application/pdf')) {
                let message = response.redirected ? 'Your session or selection needs checking. Reload this page and preview again.' : 'The PDF could not be prepared. Try again; saved challans will not be charged twice.';
                if (response.headers.get('content-type')?.includes('application/json')) message = (await response.json()).message || message;
                throw new Error(message);
            }
            const blob = await response.blob();
            if (pdfUrl) URL.revokeObjectURL(pdfUrl);
            pdfUrl = URL.createObjectURL(blob);
            ready.href = pdfUrl; ready.download = 'Fee-Challans-' + String(data.get('BillingMonth')).slice(0, 7) + '.pdf';
            ready.hidden = false; ready.click();
            status.textContent = 'Challans saved. Your PDF is ready: open it and choose Print. If the download did not start, use the link below.';
            button.textContent = 'Download challans again';
        } catch (error) {
            status.textContent = error.message || 'Download failed. Please try again.';
            button.textContent = originalText;
        } finally {
            delete commit.dataset.downloading; button.disabled = stale;
            enabled.forEach(input => input.disabled = false);
        }
    });
})();
