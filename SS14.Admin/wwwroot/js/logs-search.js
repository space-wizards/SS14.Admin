// Date picker
const datepicker = new DatePicker('litepicker', 'litepicker-close');

// Search popover
const DATA_FILTER_TAG = 'data-filter-tag';
const DATA_SEARCH_TAG = 'data-search-tag';
const DEFAULT_TAG = 'Search';
const FILTERS_KEY = 'filters';
const PER_PAGE = 'perPage';
const ROUND_ID = 'roundId';
const SORT = 'sort';

const filters = [];

const search = document.querySelector('#search');
const searchDropDown = document.querySelector('#search-popover');
let searchPopperInstance = null;

const filterTagsContainer = document.querySelector('.applied-filters');


const Tag = (filterData, id) => `
<!--suppress CssUnresolvedCustomProperty -->
<div id="${id}" class="filter-tag" ${DATA_FILTER_TAG}='${JSON.stringify(filterData)}' style="--tag-color: var(--color-filter-${filterData.key.toLowerCase()})">
                <span class="tag-prefix">${filterData.key.substr(0, 3)}:</span>
                ${filterData.value}
                <span class="tag-close fa fa-times" onclick="removeFilterTag('${id}')">x</span>
            </div>
`;


search.addEventListener('focusout', onFocusOutHandler);
search.addEventListener('focusin', show);
search.addEventListener('input', setResultText);
search.addEventListener('keydown', onSearchConfirm)

searchDropDown.addEventListener('focusout', onFocusOutHandler);

const resultFields = document.querySelectorAll('.search-result');
resultFields.forEach(field => {
    field.addEventListener('click', onSearchConfirm, true);
    field.addEventListener('keydown', onSearchConfirm);
});

if (context.parameters.has(FILTERS_KEY))
{
    const parsedFilters = JSON.parse(context.parameters.get(FILTERS_KEY));

    if (parsedFilters != null && Array.isArray(parsedFilters))
    {
        filters.push(...parsedFilters);

        filters.forEach((filter, index) => createFilterTag(filter, index));
    }
}

function create() {
    searchPopperInstance = Popper.createPopper(search, searchDropDown, {
        placement: 'bottom-start',
    });
}


function destroy() {
    if (searchPopperInstance) {
        searchPopperInstance.destroy();
        searchPopperInstance = null;
    }
}

function show() {
    searchDropDown.classList.remove('hidden');
    setResultText();
    create();
}

function hide() {
    searchDropDown.classList.add('hidden');
    destroy();
}

function onFocusOutHandler(event) {
    if (searchDropDown.contains(event.relatedTarget)
        || search.contains(event.relatedTarget)
        || search === event.explicitOriginalTarget
        || searchDropDown === event.explicitOriginalTarget) {
        return;
    }
    hide();
}

function setResultText() {
    const resultContents = document.querySelectorAll('.result-content');
    resultContents.forEach(field => field.textContent = search.value);
}

function onSearchConfirm(e)
{
    if (e.type === 'keydown' && e.key !== 'Enter')
        return false;

    let element = e.target;
    if (!element.hasAttribute(DATA_SEARCH_TAG))
        element = element.parentElement;

    const key = element.getAttribute(DATA_SEARCH_TAG) ?? DEFAULT_TAG;
    const value = search.value;

    if (!value) return;

    searchConfirmed({key: key, value: value});
    search.value = "";
    hide();
}

function searchConfirmed(filterData)
{
    if (filters.findIndex(value => value.key === filterData.key && value.value === filterData.value) >= 0) return;
    filters.push(filterData);
    const json = JSON.stringify(filters);
    context.parameters.delete(context.parameterNames.PAGE_INDEX);
    context.updateQuery(FILTERS_KEY, json);
}

function removeFilterTag(tagID) {
    const tag = document.getElementById(tagID);
    if (tag == null) return false;

    const filterData = JSON.parse(tag.getAttribute(DATA_FILTER_TAG));
    const index = filters.findIndex(value => value.key === filterData.key && value.value === filterData.value);
    filters.splice(index, 1);
    tag.parentNode.removeChild(tag);

    const json = JSON.stringify(filters);
    context.parameters.delete(context.parameterNames.PAGE_INDEX);
    context.updateQuery(FILTERS_KEY, json);
}

function createFilterTag(filterData, index)
{
    const id = 'filter-tag-' + index;
    const dom = new DOMParser().parseFromString(Tag(filterData, id), 'text/html');
    const tagElement = dom.body.firstElementChild;
    filterTagsContainer.appendChild(tagElement);
}

// Count Select
const countSelect = document.getElementById("count-select");

countSelect.addEventListener("change", OnCountSelected)

function OnCountSelected(event)
{
    context.updateQuery(PER_PAGE, event.target.value);
}

// Round ID Input
const roundIdInput = document.getElementById("round-input");

roundIdInput.addEventListener("change", OnRoundInput)

function OnRoundInput(event)
{
    context.updateQuery(ROUND_ID, event.target.value);
}

var sortSelect = document.getElementById("sort-select");
sortSelect.addEventListener("change", OnSortChange);

function OnSortChange(event) {
    context.updateQuery(SORT, event.target.value);
}
//Search key combination
document.onkeydown = function (e) {
    if (e.ctrlKey && e.key === 'f') {
        e.preventDefault();
        search.focus();
    }
};
