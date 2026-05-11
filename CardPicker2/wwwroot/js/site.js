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
