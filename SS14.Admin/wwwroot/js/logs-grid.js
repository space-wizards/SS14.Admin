const sidepanel = document.getElementById('side-panel');
const overlay = document.getElementById('side-panel-overlay');
const entries = document.querySelectorAll('.data-log');

let selecting = false;

document.addEventListener('selectstart', () => selecting = true);
entries.forEach(entry => entry.addEventListener('click', onDataGridEntryClick, true));

function onDetailsClose() {
    sidepanel.classList.add('hidden-right');
}

function onDataGridEntryClick(e) {
    if (selecting) {
        selecting = false;
        return;
    }

    const template = e.target.parentNode.querySelector('template');
    sidepanel.innerHTML = '';
    sidepanel.appendChild(document.importNode(template.content, true));
    sidepanel.classList.remove('hidden-right');
}
