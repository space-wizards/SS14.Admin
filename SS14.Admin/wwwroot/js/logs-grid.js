const sidepanel = document.getElementById('side-panel');
const overlay = document.getElementById('side-panel-overlay');
const entries = document.querySelectorAll('.data-log');

entries.forEach(entry => entry.addEventListener('click', onDataGridEntryClick, true));

overlay.addEventListener('click', () => {
    sidepanel.classList.add('hidden-right');
    overlay.classList.add('hidden');
});

function onDataGridEntryClick(e) {
    const template = e.target.parentNode.querySelector('template');
    sidepanel.innerHTML = '';
    sidepanel.appendChild(document.importNode(template.content, true));
    sidepanel.classList.remove('hidden-right');
    overlay.classList.remove('hidden');
}
