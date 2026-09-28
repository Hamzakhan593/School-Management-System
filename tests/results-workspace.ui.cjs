const {chromium}=require(process.env.CHALLAN_NODE_MODULES ? process.env.CHALLAN_NODE_MODULES+'/playwright':'playwright');
const assert=require('node:assert/strict');const path=require('node:path');const os=require('node:os');
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
for(const width of [1920,1440,1280,768,390])for(const font of [18,27]){
 await page.setViewportSize({width,height:1000});
 for(const module of ['exams','results','classresult','examdetails']){
 const response=await page.goto(`http://localhost:5299/preview?module=${module}`);assert.equal(response.status(),200,module);await page.evaluate(size=>document.documentElement.style.fontSize=size+'px',font);
 assert.doesNotMatch(await page.locator('main').innerText(), /[\uFFFD\u00C2]|\u00E2[\u20AC\u0080]/, 'readable text encoding');
 assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`${module} overflow ${width}/${font}`);
 if(module==='exams'){
 assert.equal(await page.locator('table').count(),0,'exam list uses cards');
 assert.ok(await page.locator('.exam-date').evaluateAll(es=>es.every(e=>{const r=document.createRange();r.selectNodeContents(e);return r.getClientRects().length===1 && e.scrollWidth<=e.clientWidth+1;})),`dates wrap ${width}/${font}`);
 await page.locator('details.exam-class-list summary').click();assert.equal(await page.locator('.result-chips span:visible').count(),10);
 await page.locator('#examSearch').fill('pre-board');assert.equal(await page.locator('[data-exam-name]:visible').count(),1);
 }
 if(width===1440&&font===18){if(module==='exams') await page.locator('#examSearch').fill('');await page.evaluate(()=>{document.activeElement?.blur();window.scrollTo(0,0)});await page.screenshot({path:path.join(os.tmpdir(),`results-new-${module}.png`),fullPage:true});}
 }
}
await page.setViewportSize({width:390,height:1000});await page.goto('http://localhost:5299/preview?module=classresult');assert.equal(await page.locator('#downloadSelectedResults').isDisabled(),true);await page.locator('input[name=studentIds]').first().check();assert.match(await page.locator('#resultSelectedCount').textContent(),/1 selected/);await page.locator('#selectAllResults').check();assert.match(await page.locator('#resultSelectedCount').textContent(),/12 selected/);await page.locator('#resultStudentSearch').fill('ST-001');assert.equal(await page.locator('[data-result-student]:visible').count(),1);
await page.goto('http://localhost:5299/preview?module=results');assert.match(await page.locator('a[href*="ClassResult"]').first().getAttribute('href'),/examId=3/);assert.match(await page.locator('a[href*="AllResultCardsPdf"]').getAttribute('href'),/examId=3/);assert.deepEqual(errors,[]);await browser.close();console.log('PASS exams/results/detail layouts, single-line dates, filters, selected downloads and exam-specific links at 5 widths and large text');})().catch(e=>{console.error(e);process.exit(1)});
