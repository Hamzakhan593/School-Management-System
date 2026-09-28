const { chromium } = require((process.env.CHALLAN_NODE_MODULES || '') + '/playwright');
const assert = require('node:assert/strict');
const path = require('node:path');
const os = require('node:os');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  for (const width of [1920, 1280, 768, 390]) {
    await page.setViewportSize({ width, height: 1000 });
    for (const module of ['workspace', 'marks', 'classes', 'results', 'card']) {
      const response = await page.goto(`http://localhost:5299/preview?module=${module}`);
      assert.equal(response.status(), 200, module);
      if (module === 'classes') await page.locator('[data-create=section]').click();
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), true, `${module} overflows at ${width}`);
      if (width === 1280) await page.screenshot({ path: path.join(os.tmpdir(), `academics-${module}.png`), fullPage: true });
    }
  }
  await page.setViewportSize({ width: 1280, height: 1000 });
  await page.goto('http://localhost:5299/preview?module=marks');
  assert.match(await page.locator('#marksProgress').textContent(), /4 of 12 complete/);
  const mark = page.locator('.mark-input').nth(4);
  await mark.fill('95');
  await mark.press('Enter');
  assert.equal(await page.locator('.mark-input').nth(5).evaluate(e => e === document.activeElement), true, 'Enter moves to next student');
  await page.locator('.mark-status').nth(5).selectOption('1');
  assert.equal(await page.locator('.mark-input').nth(5).isDisabled(), true);
  assert.match(await page.locator('#marksProgress').textContent(), /6 of 12 complete/);
  await page.locator('button[value=true]').click();
  assert.match(await page.locator('[data-dirty]').textContent(), /Complete the highlighted/);
  assert.equal(await page.locator('button[value=false]').textContent(), 'Save draft');
  await page.locator('#marksSearch').fill('ST-7');
  assert.equal(await page.locator('[data-mark-row]:visible').count(), 0);
  await page.locator('#marksSearch').fill('ST-007');
  assert.equal(await page.locator('[data-mark-row]:visible').count(), 1);
  page.on('dialog', d => d.accept());
  await page.goto('http://localhost:5299/preview?module=classes');
  await page.locator('[data-tab=teachers]').click();
  await page.locator('.classes-assignment:visible summary').click();
  await page.locator('#academic-16').selectOption('1');
  assert.equal(await page.locator('#academic-17 option[value="1"]').isDisabled(), false);
  assert.deepEqual(errors, []);
  await browser.close();
  console.log('PASS academic layouts at 1920/1280/768/390; marks progress, keyboard, absent status, submit guard, search and class sections');
})().catch(e => { console.error(e); process.exit(1); });
