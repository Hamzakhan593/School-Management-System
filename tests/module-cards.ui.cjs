const {chromium}=require(process.env.CHALLAN_NODE_MODULES ? process.env.CHALLAN_NODE_MODULES+'/playwright':'playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');const path=require('node:path');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});const page=await browser.newPage();await page.emulateMedia({reducedMotion:'reduce'});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const root=process.env.SETTINGS_PREVIEW_URL||'http://localhost:5299/preview';
 const output=path.join(process.env.TEMP||'.','school-module-cards');fs.mkdirSync(output,{recursive:true});
 for(const module of ['attendance','fees']){
  const response=await page.goto(root+'?module='+module);assert.equal(response.status(),200);
  if(module==='attendance')await page.locator('.attendance-tools > summary').click();
  for(const width of [1920,1440,1280,768,390]){
   await page.setViewportSize({width,height:1000});
   for(const largeText of [false,true]){
    await page.evaluate(large=>document.documentElement.style.fontSize=large?'27px':'',largeText);
    assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`${module}: page overflow at ${width}, large=${largeText}`);
    if(module==='fees'){
     const amounts=await page.locator('.fee-amount').allTextContents();assert.deepEqual(amounts,['667,000.00','477,175.00','643,850.00','98']);
     assert.equal(await page.locator('.fee-amount').evaluateAll(items=>items.some(item=>item.scrollWidth>item.clientWidth+1)),false,`Amount overflow at ${width}, large=${largeText}`);
     assert.equal(await page.locator('.fee-amount').evaluateAll(items=>items.some(item=>{const range=document.createRange();range.selectNodeContents(item);return range.getClientRects().length!==1})),false);
    }else{
     const issues=await page.locator('.attendance-method').evaluateAll(cards=>cards.flatMap(card=>{
      const box=card.getBoundingClientRect(), title=card.querySelector('h3').getBoundingClientRect(), tag=card.querySelector('.attendance-method-tag').getBoundingClientRect(), body=card.querySelector('p').getBoundingClientRect();
      const links=[...card.querySelectorAll('a')].map(a=>a.getBoundingClientRect());
      return title.top<tag.bottom-1||body.width<160||links.some(a=>a.right>box.right||a.left<box.left)?['overflow']:[];
     }));assert.deepEqual(issues,[],`Attendance layout at ${width}, large=${largeText}`);
    }
   }
   await page.evaluate(()=>document.documentElement.style.fontSize='');
  }
  await page.setViewportSize({width:1440,height:1000});
  await page.locator(module==='fees'?'.fees-summary':'.attendance-tools').screenshot({path:path.join(output,module+'-cards.png')});
 }
 await page.goto(root+'?module=attendance');await page.locator('.attendance-tools > summary').click();
 assert.equal(await page.getByRole('link',{name:'Mark attendance',exact:true}).getAttribute('href'),'#attendanceFilters');
 assert.match(await page.getByRole('link',{name:'View monthly report',exact:true}).getAttribute('href'),/academicSessionId=1/);
 assert.match(await page.getByRole('link',{name:'View monthly report',exact:true}).getAttribute('href'),/sectionId=1/);
 assert.deepEqual(errors,[]);console.log('PASS: Attendance card boundaries, badges/actions, Fees amounts remain complete on one line, 5 screen widths, larger text, existing destinations and report filters');await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
