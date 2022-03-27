/**
 * @Class
 * A date picker that lets you select a date range and appends that range as query parameters.
 */
class DatePicker {
    static PICKER_DATE_FORMAT = 'yyyy-MM-dd';

    fromDateKey = 'fromDate';
    toDateKey = 'toDate';

    /**
     * Constructs a new litepicker based date picker
     * @param pickerId the id of a text input used for the date picker
     * @param clearButtonId the id of a button that will clear the selected date range when pressed
     */
    constructor(pickerId, clearButtonId) {
        this.pickerElement = document.getElementById(pickerId);
        this.picker = new Litepicker({
                element: this.pickerElement,
                lang: 'en-US',
                singleMode: false,
                setup: (picker) =>
                {
                    picker.on('selected', () => {
                        const start = DateTime.fromJSDate(picker.getStartDate().dateInstance).toISODate();
                        const end = DateTime.fromJSDate(picker.getEndDate().dateInstance).toISODate();

                        context.parameters.set(this.fromDateKey, start);
                        context.parameters.set(this.toDateKey, end);
                        context.parameters.delete(context.parameterNames.PAGE_INDEX);
                        context.update();
                    })
                }
            });

        this.pickerElement.value = this.getDatePickerValue();

        this.pickerCloseButton = document.getElementById(clearButtonId);

        if (this.pickerCloseButton != null)
        {
            this.pickerCloseButton.addEventListener('click', () => {
                this.picker.clearSelection();
                context.parameters.delete(this.fromDateKey);
                context.parameters.delete(this.toDateKey);
                context.parameters.delete(context.parameterNames.PAGE_INDEX);
                context.update();
            });
        }
    }

    getDatePickerValue()
    {
        if (!context.parameters.has(this.fromDateKey) || !context.parameters.has(this.toDateKey)) return null;

        const from = DateTime.fromISO(context.parameters.get(this.fromDateKey));
        const to = DateTime.fromISO(context.parameters.get(this.toDateKey));

        return from.toFormat(DatePicker.PICKER_DATE_FORMAT) + ' - ' + to.toFormat(DatePicker.PICKER_DATE_FORMAT);
    }
}
