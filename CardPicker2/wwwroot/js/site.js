(() => {
  const storagePrefix = 'cardpicker.language.form.';
  const preserveForms = Array.from(document.querySelectorAll('[data-language-preserve-form]'));

  const formStorageKey = (form) => {
    const scope = form.getAttribute('data-language-preserve-form') || 'form';
    return `${storagePrefix}${window.location.pathname}:${scope}`;
  };

  const readFieldValues = (form) => {
    const values = {};
    const fields = Array.from(form.querySelectorAll('input[name], textarea[name], select[name]'));
    fields.forEach((field) => {
      if (!(field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement || field instanceof HTMLSelectElement)) {
        return;
      }

      if (field instanceof HTMLInputElement && (field.type === 'hidden' || field.name === '__RequestVerificationToken')) {
        return;
      }

      if (field instanceof HTMLInputElement && (field.type === 'checkbox' || field.type === 'radio')) {
        values[field.name] = field.checked ? field.value : '';
        return;
      }

      values[field.name] = field.value;
    });
    return values;
  };

  const restoreFieldValues = () => {
    preserveForms.forEach((form) => {
      try {
        const key = formStorageKey(form);
        const raw = window.sessionStorage?.getItem(key);
        if (!raw) {
          return;
        }

        const values = JSON.parse(raw);
        Object.entries(values).forEach(([name, value]) => {
          const fields = Array.from(form.querySelectorAll(`[name="${CSS.escape(name)}"]`));
          fields.forEach((field) => {
            if (field instanceof HTMLInputElement && (field.type === 'checkbox' || field.type === 'radio')) {
              field.checked = field.value === value;
            } else if (field instanceof HTMLInputElement || field instanceof HTMLTextAreaElement || field instanceof HTMLSelectElement) {
              field.value = value;
            }
          });
        });
        window.sessionStorage?.removeItem(key);
      } catch {
        // State preservation is best-effort and must not block form use.
      }
    });
  };

  restoreFieldValues();

  const forms = Array.from(document.querySelectorAll('[data-language-switcher]'));
  if (forms.length === 0) {
    return;
  }

  const appendValue = (url, key, value) => {
    if (value !== null && value !== undefined && value !== '') {
      url.searchParams.set(key, value);
    }
  };

  const appendRepeatedValues = (url, key, values) => {
    url.searchParams.delete(key);
    values.forEach((value) => {
      if (value !== null && value !== undefined && value !== '') {
        url.searchParams.append(key, value);
      }
    });
  };

  const preserveHomeState = (url) => {
    const drawForm = document.querySelector('[data-draw-form]');
    if (!drawForm) {
      return;
    }

    const selectedMeal = drawForm.querySelector('input[name="MealType"]:checked');
    const selectedMode = drawForm.querySelector('input[name="drawMode"]:checked');
    const operationId = drawForm.querySelector('input[name="drawOperationId"]');
    const coin = drawForm.querySelector('input[name="CoinInserted"]');
    const priceRange = drawForm.querySelector('[name="PriceRange"]');
    const preparationTimeRange = drawForm.querySelector('[name="PreparationTimeRange"]');
    const maxSpiceLevel = drawForm.querySelector('[name="MaxSpiceLevel"]');
    const tags = drawForm.querySelector('[name="Tags"]');
    const dietaryPreferences = Array.from(drawForm.querySelectorAll('input[name="DietaryPreferences"]:checked'))
      .filter((input) => input instanceof HTMLInputElement)
      .map((input) => input.value);
    const result = document.querySelector('[data-result-card-id]');

    if (selectedMode instanceof HTMLInputElement) {
      appendValue(url, 'drawMode', selectedMode.value);
    }

    if (operationId instanceof HTMLInputElement) {
      appendValue(url, 'drawOperationId', operationId.value);
    }

    if (selectedMeal instanceof HTMLInputElement) {
      appendValue(url, 'mealType', selectedMeal.value);
    }

    if (coin instanceof HTMLInputElement && coin.checked) {
      appendValue(url, 'coinInserted', 'true');
    }

    if (priceRange instanceof HTMLSelectElement) {
      appendValue(url, 'priceRange', priceRange.value);
    }

    if (preparationTimeRange instanceof HTMLSelectElement) {
      appendValue(url, 'preparationTimeRange', preparationTimeRange.value);
    }

    if (maxSpiceLevel instanceof HTMLSelectElement) {
      appendValue(url, 'maxSpiceLevel', maxSpiceLevel.value);
    }

    if (tags instanceof HTMLInputElement) {
      appendRepeatedValues(url, 'tags', tags.value.split(',').map((value) => value.trim()));
    }

    appendRepeatedValues(url, 'dietaryPreferences', dietaryPreferences);

    if (result instanceof HTMLElement) {
      appendValue(url, 'resultCardId', result.dataset.resultCardId);
    }
  };

  forms.forEach((form) => {
    form.addEventListener('submit', () => {
      preserveForms.forEach((preserveForm) => {
        try {
          window.sessionStorage?.setItem(formStorageKey(preserveForm), JSON.stringify(readFieldValues(preserveForm)));
        } catch {
          // State preservation is best-effort and must not block language switching.
        }
      });

      const returnInput = form.querySelector('input[name="returnUrl"]');
      if (!(returnInput instanceof HTMLInputElement)) {
        return;
      }

      try {
        const url = new URL(returnInput.value || `${window.location.pathname}${window.location.search}`, window.location.origin);
        preserveHomeState(url);
        returnInput.value = `${url.pathname}${url.search}${url.hash}`;
      } catch {
        returnInput.value = '/';
      }
    });
  });
})();

(() => {
  const storageKey = 'cardpicker.theme.mode';
  const allowedModes = new Set(['light', 'dark', 'system']);
  const root = document.documentElement;
  const inputs = Array.from(document.querySelectorAll('input[name="theme-mode"]'));

  const normalizeMode = (value) => allowedModes.has(value) ? value : 'system';
  let currentMode = normalizeMode(root.getAttribute('data-theme-mode'));

  const warn = (name) => {
    try {
      console.warn(name);
    } catch {
      // Console diagnostics are non-critical and must not block the page.
    }
  };

  const getSystemPreferenceQuery = () => {
    try {
      return window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;
    } catch {
      return null;
    }
  };

  const getSystemTheme = () => {
    return getSystemPreferenceQuery()?.matches ? 'dark' : 'light';
  };

  const syncSelector = (mode) => {
    inputs.forEach((input) => {
      if (input instanceof HTMLInputElement) {
        input.checked = input.value === mode;
      }
    });
  };

  const applyThemeMode = (mode) => {
    const safeMode = normalizeMode(mode);
    currentMode = safeMode;
    const effectiveTheme = safeMode === 'system' ? getSystemTheme() : safeMode;
    root.setAttribute('data-theme-mode', safeMode);
    root.setAttribute('data-bs-theme', effectiveTheme);
    syncSelector(safeMode);
  };

  const persistThemeMode = (mode) => {
    try {
      window.localStorage?.setItem(storageKey, normalizeMode(mode));
    } catch {
      warn('CardPickerThemePreferenceWriteFailed');
    }
  };

  applyThemeMode(currentMode);

  inputs.forEach((input) => {
    input.addEventListener('change', () => {
      if (input instanceof HTMLInputElement && input.checked) {
        applyThemeMode(input.value);
        persistThemeMode(input.value);
      }
    });
  });

  const systemPreferenceQuery = getSystemPreferenceQuery();
  const handleSystemPreferenceChange = () => {
    if (currentMode === 'system') {
      applyThemeMode('system');
    }
  };

  if (systemPreferenceQuery?.addEventListener) {
    systemPreferenceQuery.addEventListener('change', handleSystemPreferenceChange);
  } else if (systemPreferenceQuery?.addListener) {
    systemPreferenceQuery.addListener(handleSystemPreferenceChange);
  }

  window.addEventListener('storage', (event) => {
    try {
      if (event.key !== storageKey) {
        return;
      }

      applyThemeMode(event.newValue);
    } catch {
      warn('CardPickerThemeSyncFailed');
    }
  });
})();

(() => {
  const form = document.querySelector('[data-draw-form]');
  if (!form) {
    return;
  }

  const button = form.querySelector('[data-draw-submit]');
  const slotMachine = form.querySelector('[data-slot-machine]');
  const mealSelector = form.querySelector('[data-meal-selector]');
  const modeInputs = Array.from(form.querySelectorAll('input[name="drawMode"]'));
  const state = document.getElementById('draw-state');
  const spinningText = form.getAttribute('data-draw-state-spinning') || '';
  const reducedText = form.getAttribute('data-draw-state-reduced') || spinningText;
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  const syncMealSelector = () => {
    const selectedMode = form.querySelector('input[name="drawMode"]:checked');
    const isRandom = selectedMode instanceof HTMLInputElement && selectedMode.value === 'Random';
    if (mealSelector instanceof HTMLFieldSetElement) {
      mealSelector.disabled = isRandom || mealSelector.getAttribute('data-server-blocked') === 'true';
    }
  };

  modeInputs.forEach((input) => {
    input.addEventListener('change', syncMealSelector);
  });
  syncMealSelector();

  form.addEventListener('submit', () => {
    if (button instanceof HTMLButtonElement) {
      button.disabled = true;
    }

    if (slotMachine && !reduceMotion) {
      slotMachine.classList.add('is-spinning');
    }

    if (state && (spinningText || reducedText)) {
      state.textContent = reduceMotion ? reducedText : spinningText;
    }
  });
})();
