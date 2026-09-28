// Run from the repository root with Playwright installed (or CHALLAN_NODE_MODULES pointing to its parent).
const { chromium } = require(process.env.CHALLAN_NODE_MODULES ? process.env.CHALLAN_NODE_MODULES + '/playwright' : 'playwright');
const assert = require('node:assert/strict');
const path = require('node:path');
(async () => {
 const browser = await chromium.launch({ headless: true, channel: 'msedge' });
 const page = await browser.newPage(); const errors = [];
 page.on('pageerror', error => errors.push(error.message));
 await page.setContent(`<form method="get" id="attendanceFilters" data-sections-url="http://localhost/Attendance/Sections"><select name="academicSessionId"><option value="1">2026</option></select><select name="schoolClassId"><option value="1">Class 1</option><option value="2">Class 2</option></select><select name="sectionId"><option value="1">A</option></select><input type="date" name="attendanceDate" value="2026-09-27"><button type="submit">Show students</button></form>
 <div id="attendanceSelectionChanged" hidden>Reload</div><input id="rosterSearch" type="search"><select id="rosterFilter"><option value="">All</option><option value="0">Unmarked</option><option value="2">Absent</option></select><p id="rosterMatches"></p><p id="attendanceProgress"></p>
 <button id="markUnmarkedPresent">Remaining present</button><button id="markAllNoClass">No class</button><button id="undoAttendanceBulk" hidden>Undo</button>
 <form method="post" id="attendanceForm"><table><tbody>${['0','2','3'].map((value,index)=>`<tr data-attendance-row><td>Student ${index}</td><td><select class="attendance-status" name="Students[${index}].Status">${['0','1','2','3','7'].map(status=>`<option value="${status}" ${status===value?'selected':''}>${status}</option>`).join('')}</select></td><td><input name="Students[${index}].Remarks"></td></tr>`).join('')}</tbody></table><span id="attendanceSaveState"></span><button type="submit">Save attendance</button></form>`);
 await page.addScriptTag({path:path.resolve('School Management System/wwwroot/js/site.js')});
 await page.addScriptTag({path:path.resolve('School Management System/wwwroot/js/attendance-marking.js')});
 const values=()=>page.locator('.attendance-status').evaluateAll(fields=>fields.map(field=>field.value));
 await page.click('#markUnmarkedPresent'); assert.deepEqual(await values(),['1','2','3']);
 await page.click('#undoAttendanceBulk'); assert.deepEqual(await values(),['0','2','3']);
 await page.fill('#rosterSearch','Student 1');
 assert.equal(await page.locator('[data-attendance-row]:visible').count(),1);
 await page.click('#attendanceForm button');
 assert.equal(await page.inputValue('#rosterSearch'),'');
 assert.equal(await page.locator('[data-attendance-row]:visible').count(),3);
 assert.equal(await page.locator('#attendanceForm button').isDisabled(),false);
 await page.click('#markAllNoClass'); assert.deepEqual(await values(),['7','7','7']);
 await page.click('#undoAttendanceBulk'); assert.deepEqual(await values(),['0','2','3']);
 await page.click('#markUnmarkedPresent');
 await page.locator('.attendance-status').nth(1).selectOption('1');
 assert.equal(await page.locator('#undoAttendanceBulk').isVisible(),false);
 await page.route('http://localhost/Attendance/Sections?**',route=>route.fulfill({contentType:'application/json',body:JSON.stringify([{id:7,name:'B'}])}));
 await page.selectOption('[name="schoolClassId"]','2');
 await page.waitForFunction(()=>document.querySelector('[name="sectionId"]').value==='7');
 assert.equal(await page.locator('#attendanceForm button').isDisabled(),true);
 assert.equal(await page.locator('#attendanceSelectionChanged').isVisible(),true);
 assert.deepEqual(errors,[]);
 console.log('PASS: bulk preserves absences, undo restores values, hidden unmarked rows block saving, search, manual edits invalidate undo, section loading, stale roster lock, shared-script compatibility');
 await browser.close();
})().catch(error=>{console.error(error);process.exit(1)});
