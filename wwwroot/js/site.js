(function () {
    'use strict';

    // ---------- Loading indicator ----------
    // Forms with class "js-loading-form" disable their submit button and show a
    // spinner once browser-side validation has passed.
    document.querySelectorAll('form.js-loading-form').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            setTimeout(function () {
                if (e.defaultPrevented) { return; }
                var btn = form.querySelector('button[type="submit"]');
                if (!btn || btn.disabled) { return; }
                btn.dataset.originalHtml = btn.innerHTML;
                btn.disabled = true;
                btn.innerHTML =
                    '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Please wait...';
            }, 0);
        });
    });

    window.addEventListener('pageshow', function (ev) {
        if (!ev.persisted) { return; }
        document.querySelectorAll('form.js-loading-form button[type="submit"]').forEach(function (btn) {
            if (btn.dataset.originalHtml) {
                btn.innerHTML = btn.dataset.originalHtml;
                btn.disabled = false;
            }
        });
    });

    // ---------- Confirmation dialog ----------
    // Any form with data-confirm="message" asks for confirmation before submitting.
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || !form.getAttribute) { return; }

        var message = form.getAttribute('data-confirm');
        if (!message || form.dataset.confirmed === 'true') { return; }

        var modalEl = document.getElementById('confirmModal');
        if (!modalEl || !window.bootstrap) { return; } // fall back to a normal submit

        e.preventDefault();

        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modalEl.querySelector('.confirm-message').textContent = message;

        var okButton = modalEl.querySelector('.confirm-ok');
        okButton.onclick = function () {
            form.dataset.confirmed = 'true';
            modal.hide();
            form.submit();
        };

        modal.show();
    });
})();