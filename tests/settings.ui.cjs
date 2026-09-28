const { chromium } = require(process.env.CHALLAN_NODE_MODULES ? process.env.CHALLAN_NODE_MODULES + '/playwright' : 'playwright');
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const url = process.env.SETTINGS_PREVIEW_URL || 'http://localhost:5299/preview';
(async () => {
 const browser = await chromium.launch({ headless: true, channel: 'msedge' });
 const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
 await page.emulateMedia({ reducedMotion: 'reduce' });
 const errors = []; page.on('pageerror', error => errors.push(error.message));
 page.on('dialog', dialog => dialog.accept());
 async function load(suffix = '') { const response = await page.goto(url + suffix); assert.equal(response.status(), 200); await page.waitForFunction(() => document.getElementById('numberExamples').textContent.includes('Student')); }
 await load();
 await page.waitForFunction(() => !!window.jQuery('#settingsForm').data('validator'));
 assert.equal(await page.locator('#settingsTabs button').count(), 8);
 assert.equal(await page.locator('#settingsForm .select-search').count(), 0);
 const missingHelp = await page.locator('#settingsForm input[name]:not([type=hidden]), #settingsForm select[name], #settingsForm textarea[name]').evaluateAll(fields => fields.filter(field => !document.getElementById('help-' + field.name)).map(field => field.name));
 assert.deepEqual(missingHelp, []);
 await page.fill('#settingsSearch', 'password');
 assert.ok(await page.locator('.settings-search-result').count() >= 5);
 await page.locator('.settings-search-result').filter({hasText: 'Minimum password characters'}).click();
 assert.equal(await page.locator('#security').isVisible(), true);
 await page.fill('#PasswordRequiredLength', '12');
 assert.match(await page.locator('#settingsSaveState').textContent(), /1 unsaved/);
 await page.fill('#PasswordRequiredLength', '8');
 assert.equal(await page.locator('#settingsSaveState').textContent(), 'No unsaved changes');
 await page.fill('#settingsSearch', 'zzzznomatch');
 assert.match(await page.locator('#settingsSearchStatus').textContent(), /No matching/);
 await page.fill('#settingsSearch', '');
 await page.locator('[data-bs-target="#numbering"]').click();
 await page.fill('#AdmissionNumberPrefix', 'ABC');
 assert.match(await page.locator('#numberExamples').textContent(), /ABC-\d{4}-0001/);
 await page.locator('[data-bs-target="#fees"]').click(); await page.check('#AutoGenerateMonthlyChallans'); await page.fill('#MonthlyChallanGenerationDay', '5');
 assert.match(await page.locator('#feeRuleSummary').textContent(), /from day 5/);
 await page.locator('[data-bs-target="#security"]').click(); await page.fill('#SessionTimeoutMinutes', '1');
 await page.locator('[data-bs-target="#numbering"]').click();
 await page.locator('.settings-savebar button').click();
 assert.equal(await page.locator('#security').isVisible(), true);
 assert.equal(await page.locator('.settings-savebar button').isDisabled(), false);
 await page.fill('#SessionTimeoutMinutes', '30');
 await page.evaluate(() => document.getElementById('settingsForm').addEventListener('submit', event => { event.preventDefault(); window.settingsPayload = [...new FormData(event.currentTarget)]; }));
 await page.locator('.settings-savebar button').click();
 assert.equal(await page.locator('.settings-savebar button').isDisabled(), true);
 const payload = await page.evaluate(() => window.settingsPayload);
 assert.ok(payload.some(([key, value]) => key === 'AdmissionNumberPrefix' && value === 'ABC'));
 assert.ok(payload.some(([key, value]) => key === 'MonthlyChallanGenerationDay' && value === '5'));
 assert.ok(payload.some(([key, value]) => key === 'SelectedSection' && value === 'security'));
 console.log('PASS: every setting has help, cross-category search, changes tracking, live examples, hidden-field validation and all-category form submission');
 await load('?error=1'); assert.equal(await page.locator('#security').isVisible(), true);
 assert.match(await page.locator('#settingsSaveState').textContent(), /Changes not saved/);
 await load('?tab=backup'); assert.equal(await page.locator('#backup').isVisible(), true);
 const output = path.join(process.env.TEMP || '.', 'school-settings-preview'); fs.mkdirSync(output,{recursive:true});
 for (const width of [1920, 1440, 1280, 768, 390]) {
   await page.setViewportSize({width,height:1000});
   for (const section of ['numbering','fees','attendance','print','backup','security','integrations','masterdata']) {
     await page.locator(`[data-bs-target="#${section}"]`).click();
     assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${section} overflows at ${width}px`);
     const cramped = await page.locator(`#${section} .form-check`).evaluateAll(cards => cards.flatMap(card => {
       const help = card.querySelector('.settings-help'), label = card.querySelector('.form-check-label'), input = card.querySelector('.form-check-input'), state = card.querySelector('.settings-toggle-state');
       if (!help || !label || !input || !state) return [];
       const h = help.getBoundingClientRect(), l = label.getBoundingClientRect(), i = input.getBoundingClientRect(), badge = state.getBoundingClientRect();
       return h.width < 160 || l.top < i.bottom - 1 || l.top < badge.bottom - 1 ? [label.textContent] : [];
     }));
     assert.deepEqual(cramped, [], `Cramped toggles in ${section} at ${width}px`);
     assert.equal(await page.locator('.settings-savebar').evaluate(element => getComputedStyle(element).position), 'static');
     if (section === 'print') {
       const positions = await page.locator('#PayslipFooterText, #GeneralPrintFooterText').evaluateAll(fields => fields.map(field => { const box = field.getBoundingClientRect(); return {top:box.top,left:box.left}; }));
       if (positions[0].left !== positions[1].left) assert.ok(Math.abs(positions[0].top - positions[1].top) < 2, 'Print fields are misaligned');
     }


   }
   await page.locator('[data-bs-target="#fees"]').click();
   await page.screenshot({path:path.join(output,`settings-fees-${width}.png`),fullPage:true});
 }
 await page.setViewportSize({width:1280,height:1000});
 await page.evaluate(() => document.documentElement.style.fontSize = '27px');
 for (const section of ['attendance','print','security','integrations']) {
   await page.locator(`[data-bs-target="#${section}"]`).click();
   assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `Large-text overflow in ${section}`);
   assert.equal(await page.locator(`#${section} .form-check .settings-help`).evaluateAll(items => items.some(item => item.getBoundingClientRect().width < 160)), false);
 }
 await page.evaluate(() => document.documentElement.style.fontSize = '');
 await page.setViewportSize({width:1440,height:1000});
 const sidebarLink = page.locator('.sms-sidebar .sms-nav-link').filter({hasText:'Dashboard'});
 const normalBackground = await sidebarLink.evaluate(element => getComputedStyle(element).backgroundColor);
 assert.notEqual(normalBackground, 'rgba(0, 0, 0, 0)');
 await sidebarLink.hover(); assert.notEqual(await sidebarLink.evaluate(element => getComputedStyle(element).borderColor), 'rgba(0, 0, 0, 0)');
 assert.equal(await page.locator('.sms-sidebar a[aria-current="page"]').count(), 1);
 for (const section of ['attendance','print']) {
   await page.locator(`[data-bs-target="#${section}"]`).click();
   await page.evaluate(() => scrollTo(0, 0));
   await page.screenshot({path:path.join(output,`settings-${section}-full-theme.png`),fullPage:true});
 }
 await page.setViewportSize({width:390,height:900});
 await page.getByRole('button',{name:'Open navigation'}).click();
 await page.waitForFunction(() => document.getElementById('smsMobileNav').classList.contains('show'));
 assert.equal(await page.locator('#smsMobileNav a[aria-current="page"]').count(), 1);
 await page.screenshot({path:path.join(output,'settings-mobile-navigation.png')});
 assert.deepEqual(errors, []);
 console.log('PASS: server errors open the correct category, selected category restored, all eight categories fit desktop/tablet/mobile, no JavaScript errors');
 await browser.close();
})().catch(error => { console.error(error); process.exit(1); });
