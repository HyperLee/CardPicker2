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
  const state = document.getElementById('draw-state');
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  form.addEventListener('submit', () => {
    if (button instanceof HTMLButtonElement) {
      button.disabled = true;
    }

    if (slotMachine && !reduceMotion) {
      slotMachine.classList.add('is-spinning');
    }

    if (state) {
      state.textContent = reduceMotion ? '正在揭示結果。' : '轉動中，請稍候。';
    }
  });
})();
