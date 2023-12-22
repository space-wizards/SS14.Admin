document.addEventListener('DOMContentLoaded', function () {
    const LogType = {
        Unknown: 0,
        Damaged: 2,
        Healed: 3,
        Slip: 4,
        EventAnnounced: 5,
        EventStarted: 6,
        EventStopped: 7,
        ShuttleCalled: 8,
        ShuttleRecalled: 9,
        ExplosiveDepressurization: 10,
        Respawn: 13,
        RoundStartJoin: 14,
        LateJoin: 15,
        EventRan: 16,
        ChemicalReaction: 17,
        ReagentEffect: 18,
        Verb: 19,
        CanisterValve: 20,
        CanisterPressure: 21,
        CanisterPurged: 22,
        CanisterTankEjected: 23,
        CanisterTankInserted: 24,
        DisarmedAction: 25,
        DisarmedKnockdown: 26,
        AttackArmedClick: 27,
        AttackArmedWide: 28,
        AttackUnarmedClick: 29,
        AttackUnarmedWide: 30,
        InteractHand: 31,
        InteractActivate: 32,
        Throw: 33,
        Landed: 34,
        ThrowHit: 35,
        Pickup: 36,
        Drop: 37,
        BulletHit: 38,
        CrayonDraw: 39,
        MeleeHit: 41,
        HitScanHit: 42,
        Mind: 43,
        Explosion: 44,
        Radiation: 45,
        Barotrauma: 46,
        Flammable: 47,
        Asphyxiation: 48,
        Temperature: 49,
        Hunger: 50,
        Thirst: 51,
        Electrocution: 52,
        ForceFeed: 40,
        Ingestion: 53,
        AtmosPressureChanged: 54,
        AtmosPowerChanged: 55,
        AtmosVolumeChanged: 56,
        AtmosFilterChanged: 57,
        AtmosRatioChanged: 58,
        FieldGeneration: 59,
        GhostRoleTaken: 60,
        Chat: 61,
        Action: 62,
        RCD: 63,
        Construction: 64,
        Trigger: 65,
        Anchor: 66,
        Unanchor: 67,
        EmergencyShuttle: 68,
        Emag: 69,
        Gib: 70,
        Identity: 71,
        CableCut: 72,
        StorePurchase: 73,
        LatticeCut: 74,
        Stripping: 75,
        Stamina: 76,
        EntitySpawn: 77,
        AdminMessage: 78,
        Anomaly: 79,
        WireHacking: 80,
        Teleport: 81,
        EntityDelete: 82,
        Vote: 83,
        ItemConfigure: 84,
        DeviceLinking: 85,
        Tile: 86
    };

    const x = document.getElementById('logTypeInput');
    const autocompleteOptions = document.getElementById('autocompleteOptions');
    let currentSelectedIndex = -1; // To keep track of the selected index

    // Check if context parameter "type" exists and set the value if present
    if (context.parameters.has("type")) {
        x.value = context.parameters.get("type");
    }

    // Add change event listener to update context parameter "type" on input change
    x.addEventListener('change', function() {
        var currentValue = x.value;
        context.updateQuery("type", currentValue);
    });

    x.addEventListener('input', function () {
        const currentInput = this.value.toLowerCase();
        const filteredValues = Object.keys(LogType).filter(
            (key) => key.toLowerCase().includes(currentInput)
        );

        showAutocompleteOptions(filteredValues);
        highlightMatchingOption();
    });

    x.addEventListener('blur', function () {
        setTimeout(hideAutocompleteOptions, 200);
    });

    x.addEventListener('focus', function () {
        const currentInput = this.value.toLowerCase();
        if (currentInput.length > 0) {
            const filteredValues = Object.keys(LogType).filter(
                (key) => key.toLowerCase().includes(currentInput)
            );
            showAutocompleteOptions(filteredValues);
        }
    });

    x.addEventListener('keydown', function (event) {
        if (event.key === 'Tab') {
            const suggestions = autocompleteOptions.querySelectorAll('div');
            currentSelectedIndex++;
            if (currentSelectedIndex >= 0 && currentSelectedIndex < suggestions.length) {
                handleSelectedOption(suggestions);
                event.preventDefault(); // Prevent tab from moving to the next input
            } else if (currentSelectedIndex >= suggestions.length) {
                // Reset to the top of the list
                currentSelectedIndex = 0;
                handleSelectedOption(suggestions);
                event.preventDefault(); // Prevent tab from moving to the next input
            }
        } else if (event.key === 'Enter') {
            const suggestions = autocompleteOptions.querySelectorAll('div');
            if (currentSelectedIndex !== -1 && currentSelectedIndex < suggestions.length) {
                x.value = suggestions[currentSelectedIndex].textContent;
                var currentValue = x.value;
                context.updateQuery("type", currentValue);
                hideAutocompleteOptions();
                event.preventDefault(); // Prevent form submission
            } else if (x.value && suggestions.length > 0) {
                x.value = suggestions[0].textContent;
                var currentValue = x.value;
                context.updateQuery("type", currentValue);
                hideAutocompleteOptions();
                event.preventDefault(); // Prevent form submission
            }
        }
    });

    document.addEventListener('click', function (event) {
        if (event.target !== x && !autocompleteOptions.contains(event.target)) {
            hideAutocompleteOptions();
        }
    });

    autocompleteOptions.addEventListener('click', function (event) {
        if (event.target.tagName === 'DIV') {
            x.value = event.target.textContent;
            var currentValue = x.value;
            context.updateQuery("type", currentValue);
            hideAutocompleteOptions();
        }
    });

    function showAutocompleteOptions(values) {
        autocompleteOptions.innerHTML = '';

        values.slice(0, 5).forEach((value) => {
            const option = document.createElement('div');
            option.textContent = value;
            autocompleteOptions.appendChild(option);
        });

        autocompleteOptions.style.display = 'block';
    }

    function hideAutocompleteOptions() {
        autocompleteOptions.style.display = 'none';
        currentSelectedIndex = -1; // Reset the selected index when hiding options
        const selectedOption = autocompleteOptions.querySelector('.selected');
        if (selectedOption) {
            selectedOption.classList.remove('selected');
        }
    }

    function handleSelectedOption(suggestions) {
        suggestions.forEach((option, index) => {
            if (index === currentSelectedIndex) {
                option.classList.add('selected');
                x.value = option.textContent;
            } else {
                option.classList.remove('selected');
            }
        });
    }

    function highlightMatchingOption() {
        const inputText = x.value.toLowerCase();
        const suggestions = autocompleteOptions.querySelectorAll('div');

        suggestions.forEach((option) => {
            if (option.textContent.toLowerCase() === inputText) {
                option.classList.add('highlighted');
            } else {
                option.classList.remove('highlighted');
            }
        });
    }

    // Disable click event on autocomplete options
    autocompleteOptions.addEventListener('click', function (event) {
        event.preventDefault();
    });
});
