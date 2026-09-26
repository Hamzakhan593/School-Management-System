const { chromium } = require(process.env.CHALLAN_NODE_MODULES ? process.env.CHALLAN_NODE_MODULES + '/playwright' : 'playwright');
const path = require('node:path');
const assert = require('node:assert/strict');
(async () => {
const browser = await chromium.launch({headless:true, channel:'msedge'});
const page = await browser.newPage();
const errors=[]; page.on('pageerror',e=>errors.push(e.message));
const fixture=`<div class="challan-workspace"><form id="generationForm" method="post" action="http://localhost/Preview">
${['Individual','ClassSection','WholeSchool','SelectedStudents'].map(s=>`<label for="s${s}"><input type="radio" name="Scope" id="s${s}" value="${s}" ${s==='WholeSchool'?'checked':''}>${s}</label>`).join('')}
<div data-scope="Individual"><input type="search" id="studentSearch"><select id="StudentId" name="StudentId"><option value="">Choose</option><option value="1">Ali A001</option><option value="2">Sara A002</option></select></div>
<div data-scope="ClassSection"><select id="SchoolClassId" name="SchoolClassId"><option value="">Choose</option><option value="10">Class 1</option><option value="20">Class 2</option></select><select id="SectionId" name="SectionId"><option value="">All sections</option><option value="100" data-class="10">A</option><option value="200" data-class="20">B</option></select></div>
<div data-scope="WholeSchool">All students</div><div data-scope="SelectedStudents"><input type="search" id="multiSearch"><span id="selectionCount"></span><button type="button" id="clearStudents">Clear</button><label for="c1"><input id="c1" name="SelectedStudentIds" type="checkbox" value="1">Ali</label><label for="c2"><input id="c2" name="SelectedStudentIds" type="checkbox" value="2">Sara</label><p id="noStudentMatches" hidden>No matches</p></div>
<input type="month" id="billingMonthPicker" value="2026-09"><input id="billingMonthValue" name="BillingMonth" type="hidden"><button type="submit">Check</button></form>
<section id="challanPreview"><h3 id="previewTitle" tabindex="-1">Preview</h3><div id="previewChanged" hidden>Changed</div><form id="challanCommitForm" method="post" action="http://localhost/Commit"><input name="BillingMonth" value="2026-09-01" type="hidden"><button type="submit">Download</button></form><p id="downloadStatus"></p><a id="readyPdf" hidden>PDF</a></section></div>`;
async function setup(){ await page.setContent(fixture); await page.addScriptTag({path:path.resolve('School Management System/wwwroot/js/site.js')}); await page.addScriptTag({path:path.resolve('School Management System/wwwroot/js/challan-generation.js')}); }
await setup();
assert.equal(await page.locator('#StudentId').isDisabled(),true);
await page.locator('#sClassSection').check();
await page.selectOption('#SchoolClassId','10'); await page.selectOption('#SectionId','100');
await page.selectOption('#SchoolClassId','20');
assert.equal(await page.inputValue('#SectionId'),'');
assert.equal(await page.locator('#SectionId option[value="100"]').isDisabled(),true);
assert.equal(await page.locator('#challanCommitForm button').isDisabled(),true);
await page.locator('#sSelectedStudents').check();
await page.locator('#generationForm button[type="submit"]').click();
assert.match(await page.locator('#selectionCount').textContent(),/at least one/);
assert.equal(await page.locator('#generationForm').getAttribute('data-saving'),null);
await page.locator('#c1').check(); await page.fill('#multiSearch','Sara');
assert.equal(await page.locator('#c1').isChecked(),true);
await page.click('#clearStudents'); assert.equal(await page.locator('#c1').isChecked(),false);
console.log('PASS: scope controls, dependent sections, stale preview protection, empty selection, search preserves selection, clear selection, global script compatibility');
await setup();
await page.route('http://localhost/Commit',route=>route.fulfill({status:400,contentType:'application/json',body:JSON.stringify({message:'Please preview again.'})}));
await page.click('#challanCommitForm button');
await page.waitForFunction(()=>document.getElementById('downloadStatus').textContent==='Please preview again.');
assert.equal(await page.locator('#challanCommitForm button').isDisabled(),false);
await page.unroute('http://localhost/Commit');
await page.route('http://localhost/Commit',route=>route.fulfill({status:200,contentType:'application/pdf',body:'%PDF-1.4\n%%EOF'}));
await page.click('#challanCommitForm button');
await page.waitForFunction(()=>!document.getElementById('readyPdf').hidden);
assert.equal(await page.locator('#challanCommitForm button').textContent(),'Download challans again');
assert.equal(await page.locator('#StudentId').isDisabled(),true);
assert.deepEqual(errors,[]);
console.log('PASS: error message, retry, PDF download fallback, controls restored, no JavaScript errors');
await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
