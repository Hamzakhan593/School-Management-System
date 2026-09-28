const {chromium}=require(process.env.CHALLAN_NODE_MODULES ? process.env.CHALLAN_NODE_MODULES + '/playwright' : 'playwright');
const assert=require('node:assert/strict');
const os=require('node:os');const path=require('node:path');
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});const page=await browser.newPage();
for(const width of [1920,1440,1280,768,390]){for(const font of [18,27]){
await page.setViewportSize({width,height:1000});await page.goto('http://localhost:5299/preview?module=classes');await page.evaluate(size=>document.documentElement.style.fontSize=size+'px',font);
assert.equal(await page.locator('[data-create-panel]:visible').count(),0);
for(const key of ['class','section','group','subject']){await page.locator(`[data-create=${key}]`).click();assert.equal(await page.locator('[data-create-panel]:visible').count(),1);assert.equal(await page.locator(`[data-create-panel=${key}]`).isVisible(),true);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`${key} overflow ${width}/${font}`);
const inputs=await page.locator('[data-create-panel]:visible .classes-fields input:not([type=hidden]):not([type=checkbox]),[data-create-panel]:visible .classes-fields select').evaluateAll(es=>es.map(e=>({x:e.getBoundingClientRect().x,y:e.getBoundingClientRect().y,w:e.getBoundingClientRect().width,h:e.getBoundingClientRect().height})));for(let i=0;i<inputs.length;i++)for(let j=i+1;j<inputs.length;j++){const a=inputs[i],b=inputs[j];assert.ok(!(a.x<b.x+b.w-1&&a.x+a.w-1>b.x&&a.y<b.y+b.h-1&&a.y+a.h-1>b.y),'overlapping inputs');}
}
await page.locator('[data-close-create]:visible').click();assert.equal(await page.locator('[data-create-panel]:visible').count(),0);
for(const tab of ['subjects','teachers','classes']){await page.locator(`[data-tab=${tab}]`).click();assert.equal(await page.locator('[data-tab-panel]:visible').count(),1);assert.equal(await page.locator(`[data-tab-panel=${tab}]`).isVisible(),true);}
}}
await page.setViewportSize({width:1440,height:1000});await page.goto('http://localhost:5299/preview?module=classes');await page.screenshot({path:path.join(os.tmpdir(),'classes-new-overview.png'),fullPage:true});await page.locator('[data-create=section]').click();await page.screenshot({path:path.join(os.tmpdir(),'classes-new-section.png'),fullPage:true});await page.locator('[data-create-panel=section] input[name=Name]').fill('C');await page.locator('[data-create=class]').click();await page.locator('[data-create=section]').click();assert.equal(await page.locator('[data-create-panel=section] input[name=Name]').inputValue(),'C');
await browser.close();console.log('PASS class creation chooser, tabs, retained inputs, no overlap/overflow at 5 widths and 2 text sizes');})().catch(e=>{console.error(e);process.exit(1)});
