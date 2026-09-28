(() => {
 const search = document.getElementById('examSearch');
 search?.addEventListener('input', () => {
  let count = 0;
  document.querySelectorAll('[data-exam-name]').forEach(card => { card.hidden = !card.dataset.examName.toLowerCase().includes(search.value.trim().toLowerCase()); if (!card.hidden) count++; });
  document.getElementById('examSearchEmpty').hidden = count > 0;
 });
 if (location.hash.startsWith('#exam-')) {
  const exam = document.getElementById(location.hash.slice(1));
  if (exam?.matches('details.result-exam')) exam.open = true;
 }
 const studentSearch = document.getElementById('resultStudentSearch');
 studentSearch?.addEventListener('input', () => {
  let count=0;
  document.querySelectorAll('[data-result-student]').forEach(row => { row.hidden = !row.dataset.resultStudent.toLowerCase().includes(studentSearch.value.trim().toLowerCase()); if(!row.hidden) count++; });
  document.getElementById('resultSearchCount').textContent = `${count} students shown`;
 });
 const selectAll = document.getElementById('selectAllResults');
 const boxes = [...document.querySelectorAll('input[name=studentIds]')];
 function selection() {
  const selected = boxes.filter(x=>x.checked).length;
  const output=document.getElementById('resultSelectedCount'); if(output) output.textContent=`${selected} selected`;
  const button=document.getElementById('downloadSelectedResults'); if(button) button.disabled=selected===0;
  if(selectAll){selectAll.checked=boxes.length>0&&selected===boxes.length;selectAll.indeterminate=selected>0&&selected<boxes.length;}
 }
 selectAll?.addEventListener('change',()=>{boxes.forEach(x=>x.checked=selectAll.checked);selection();});
 boxes.forEach(x=>x.addEventListener('change',selection));selection();
})();
