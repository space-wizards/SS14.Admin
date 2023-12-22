const sidepanel = document.getElementById('side-panel');
const overlay = document.getElementById('side-panel-overlay');
const entries = document.querySelectorAll('.data-log');

entries.forEach(entry => entry.addEventListener('click', onDataGridEntryClick));

function onDetailsClose() {
    sidepanel.classList.add('hidden-right');
}
function onDataGridEntryClick(e) {
    const template = e.currentTarget.querySelector('template');
    sidepanel.innerHTML = '';
    sidepanel.appendChild(document.importNode(template.content, true));
    sidepanel.classList.remove('hidden-right');
}

overlay.addEventListener('click', () => {
    sidepanel.classList.add('hidden-right');
});
