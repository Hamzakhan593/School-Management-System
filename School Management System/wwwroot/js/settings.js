(() => {
    const form = document.getElementById('settingsForm');
    if (!form) return;
    const tabs = [...document.querySelectorAll('#settingsTabs [data-bs-target]')];
    const fields = [...form.querySelectorAll('input[name]:not([type="hidden"]), select[name], textarea[name]')];
    const section = form.querySelector('[name="SelectedSection"]');
    const saveState = document.getElementById('settingsSaveState');
    const saveButtons = [...document.querySelectorAll('button[type="submit"]')].filter(button => button.form === form);
    const originallyInvalid = form.dataset.hasErrors === 'true';
    let submitting = false;
    const valueOf = field => field.type === 'checkbox' ? field.checked : field.type === 'file' ? [...field.files].map(file => `${file.name}:${file.size}:${file.lastModified}`).join('|') : field.value;
    const originals = new Map(fields.map(field => [field, valueOf(field)]));
    const changed = () => fields.filter(field => valueOf(field) !== originals.get(field));
    const buttonFor = id => tabs.find(tab => tab.dataset.bsTarget === '#' + id);
    function showSection(id) {
        const tab = buttonFor(id) || tabs[0];
        bootstrap.Tab.getOrCreateInstance(tab).show();
        section.value = tab.dataset.bsTarget.slice(1);
    }
    function reveal(field) {
        const pane = field?.closest('.tab-pane');
        if (!pane) return;
        showSection(pane.id);
        field.focus({ preventScroll: true });
        field.scrollIntoView({ block: 'center', behavior: 'auto' });
    }
    tabs.forEach((tab, index) => {
        const id = tab.dataset.bsTarget.slice(1), pane = document.getElementById(id);
        tab.id = 'settings-tab-' + id;
        tab.setAttribute('role', 'tab'); tab.setAttribute('aria-controls', id);
        tab.setAttribute('aria-selected', tab.classList.contains('active') ? 'true' : 'false');
        tab.tabIndex = tab.classList.contains('active') ? 0 : -1;
        pane.setAttribute('role', 'tabpanel'); pane.setAttribute('aria-labelledby', tab.id);
        tab.addEventListener('shown.bs.tab', () => {
            section.value = id;
            tabs.forEach(other => { other.tabIndex = other === tab ? 0 : -1; other.setAttribute('aria-selected', String(other === tab)); });
            history.replaceState(null, '', '#' + id);
        });
        tab.addEventListener('keydown', event => {
            if (!['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End'].includes(event.key)) return;
            event.preventDefault(); event.stopPropagation();
            const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (index + (['ArrowRight', 'ArrowDown'].includes(event.key) ? 1 : -1) + tabs.length) % tabs.length;
            showSection(tabs[next].dataset.bsTarget.slice(1)); tabs[next].focus();
        });
    });
    const checked = id => document.getElementById(id)?.checked;
    const value = id => document.getElementById(id)?.value || '';
    const previewNumber = (prefix, digits) => `${value(prefix).trim().toUpperCase() || '…'}-${new Date().getFullYear()}-${'1'.padStart(Math.max(1, Math.min(10, Number(value(digits)) || 1)), '0')}`;
    function update() {
        const edits = changed();
        saveState.textContent = submitting ? 'Saving settings…' : originallyInvalid ? 'Changes not saved — check the highlighted fields' : edits.length ? `${edits.length} unsaved setting${edits.length === 1 ? '' : 's'}` : 'No unsaved changes';
        tabs.forEach(tab => {
            const pane = document.getElementById(tab.dataset.bsTarget.slice(1));
            tab.classList.toggle('settings-has-changes', edits.some(field => pane.contains(field)));
        });
        fields.filter(field => field.type === 'checkbox').forEach(field => {
            let indicator = field.closest('.form-check')?.querySelector('.settings-toggle-state');
            if (!indicator && field.closest('.form-check')) {
                indicator = document.createElement('span'); indicator.className = 'settings-toggle-state'; indicator.setAttribute('aria-hidden', 'true'); field.closest('.form-check').append(indicator);
            }
            if (indicator) { indicator.textContent = field.checked ? 'On' : 'Off'; indicator.dataset.on = String(field.checked); }
        });
        const examples = document.getElementById('numberExamples');
        if (examples) examples.textContent = `Format examples (not the next available numbers): Student ${previewNumber('AdmissionNumberPrefix', 'AdmissionNumberDigits')} · Challan ${previewNumber('ChallanNumberPrefix', 'FinancialNumberDigits')} · Receipt ${previewNumber('ReceiptNumberPrefix', 'FinancialNumberDigits')}`;
        const fees = document.getElementById('feeRuleSummary');
        if (fees) fees.textContent = `Fee payment is due on day ${value('DefaultFeeDueDay') || '…'} of each month. ` + (checked('AutoGenerateMonthlyChallans') ? `Automatic challans: from day ${value('MonthlyChallanGenerationDay') || '…'} while the app is running. ` : 'Automatic challans are off. ') + (checked('ApplyLateFeeOnCollection') && Number(value('LateFeeFixedAmount')) > 0 ? `Late fee: PKR ${value('LateFeeFixedAmount')} after ${value('LateFeeGraceDays') || '0'} extra day(s).` : 'No new automatic late fee with these settings.');
        for (const [toggle, dependent] of [['AutoGenerateMonthlyChallans','MonthlyChallanGenerationDay'], ['ScheduledBackupsEnabled','BackupHourLocal']]) {
            const input = document.getElementById(dependent);
            if (input) input.closest('[class*="col-"]')?.classList.toggle('settings-inactive-option', !checked(toggle));
        }
    }
    fields.forEach(field => {
        if (field.dataset.valRangeMin) field.min = field.dataset.valRangeMin;
        if (field.dataset.valRangeMax) field.max = field.dataset.valRangeMax;
        if (field.dataset.valLengthMax) field.maxLength = Number(field.dataset.valLengthMax);
        if (field.dataset.valRequired && field.type !== 'checkbox') field.required = true;
        field.addEventListener('input', update); field.addEventListener('change', update);
    });
    const search = document.getElementById('settingsSearch');
    const results = document.getElementById('settingsSearchResults');
    const searchStatus = document.getElementById('settingsSearchStatus');
    const entries = fields.map(field => {
        const pane = field.closest('.tab-pane');
        const title = (field.labels?.[0]?.textContent || field.name).replace(/\s*· required$/, '').trim();
        const help = document.getElementById('help-' + field.name)?.textContent || '';
        const category = buttonFor(pane?.id)?.textContent || '';
        return {field, title, category, text: `${title} ${help} ${category}`.toLowerCase()};
    });
    search.addEventListener('input', () => {
        const terms = search.value.trim().toLowerCase().split(/\s+/).filter(Boolean);
        results.replaceChildren(); results.hidden = !terms.length;
        if (!terms.length) { searchStatus.textContent = 'Search across all categories. Your edits stay in place when you switch categories.'; return; }
        const matches = entries.filter(entry => terms.every(term => entry.text.includes(term)));
        searchStatus.textContent = matches.length ? `${matches.length} setting(s) found. Choose one to open its category.` : 'No matching settings. Try a simpler word such as fees, signature or password.';
        matches.forEach(entry => {
            const button = document.createElement('button'); button.type = 'button'; button.className = 'settings-search-result';
            const title = document.createElement('strong'); title.textContent = entry.title;
            const category = document.createElement('span'); category.textContent = entry.category;
            button.append(title, category); button.addEventListener('click', () => reveal(entry.field)); results.append(button);
        });
    });
    search.addEventListener('keydown', event => { if (event.key === 'Enter') { event.preventDefault(); results.querySelector('button')?.click(); } });
    form.noValidate = true;
    form.addEventListener('submit', event => {
        if (submitting) { event.preventDefault(); event.stopImmediatePropagation(); return; }
        const validator = window.jQuery?.(form).data('validator');
        if (validator) validator.settings.ignore = ':disabled';
        const nativeValid = form.checkValidity();
        const rulesValid = !validator || validator.form();
        if (!nativeValid || !rulesValid) {
            event.preventDefault(); event.stopImmediatePropagation();
            const invalid = fields.find(field => !field.validity.valid) || validator?.errorList[0]?.element;
            reveal(invalid); invalid?.reportValidity();
            saveState.textContent = 'Not saved — check the highlighted setting.';
            return;
        }
        submitting = true; form.setAttribute('aria-busy', 'true');
        saveButtons.forEach(button => { button.dataset.settingsOriginalText = button.textContent; button.disabled = true; button.textContent = 'Saving…'; });
        update();
    }, true);
    window.addEventListener('beforeunload', event => { if (!submitting && (changed().length || originallyInvalid)) { event.preventDefault(); event.returnValue = ''; } });
    window.addEventListener('pageshow', () => {
        submitting = false; form.removeAttribute('aria-busy');
        saveButtons.forEach(button => { button.disabled = false; if (button.dataset.settingsOriginalText) button.textContent = button.dataset.settingsOriginalText; }); update();
    });
    showSection(buttonFor(location.hash.slice(1)) ? location.hash.slice(1) : section.value);
    const serverError = form.querySelector('.input-validation-error');
    if (serverError) reveal(serverError);
    update();
})();
