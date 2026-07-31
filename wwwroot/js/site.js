// Sidebar collapse/expand, toggled by the navbar burger button.
// The chosen state is remembered across page loads so navigating doesn't reset it.
(function () {
    var STORAGE_KEY = 'rpms.sidebar.collapsed';

    var toggle = document.getElementById('sidebar-toggle');
    var sidebar = document.getElementById('rpms-sidebar');

    if (!toggle || !sidebar) return;

    function apply(collapsed) {
        sidebar.classList.toggle('rpms-sidebar-collapsed', collapsed);
        // Also drives the burger's morph into an X (see .rpms-burger-* in site.css).
        toggle.setAttribute('aria-expanded', String(!collapsed));
    }

    // Restore the previous state before the user interacts.
    apply(localStorage.getItem(STORAGE_KEY) === 'true');

    toggle.addEventListener('click', function () {
        var collapsed = !sidebar.classList.contains('rpms-sidebar-collapsed');
        apply(collapsed);
        localStorage.setItem(STORAGE_KEY, String(collapsed));
    });
})();

// Toasts (markup comes from _Toasts.cshtml). Hand-rolled rather than using Bootstrap's Toast:
// Tabler bundles its own Bootstrap copy and doesn't expose `window.bootstrap`, so its JS API
// isn't reachable from here.
//
// Exposes window.rpmsToast(kind, messages) so client-side validation can raise a toast without
// a round trip, reusing the <template> the partial rendered instead of duplicating its markup.
window.rpmsToast = (function () {
    var DEFAULT_DELAY = 5000;
    var EXIT_ANIMATION_MS = 250;

    function dismiss(toast) {
        if (toast.dataset.dismissing) return;   // ignore repeat clicks mid-exit
        toast.dataset.dismissing = 'true';

        toast.classList.add('rpms-toast-leaving');
        window.setTimeout(function () { toast.remove(); }, EXIT_ANIMATION_MS);
    }

    // Wires up a toast that is already in the document.
    function activate(toast) {
        if (toast.dataset.toastReady) return;
        toast.dataset.toastReady = 'true';

        // Appearing is CSS's job (see .rpms-toast) — this only schedules the exit.
        var delay = parseInt(toast.dataset.toastDelay, 10) || DEFAULT_DELAY;
        var timer = null;

        function startTimer() {
            // If the page opened in a background tab, hold the message until it's actually
            // on screen — otherwise it would time out unseen.
            if (document.hidden) return;
            timer = window.setTimeout(function () { dismiss(toast); }, delay);
        }

        startTimer();
        if (document.hidden) {
            document.addEventListener('visibilitychange', function onShow() {
                if (document.hidden) return;
                document.removeEventListener('visibilitychange', onShow);
                startTimer();
            });
        }

        // Don't let a message vanish while it's being read.
        toast.addEventListener('mouseenter', function () { window.clearTimeout(timer); });
        toast.addEventListener('mouseleave', startTimer);

        var closeButton = toast.querySelector('.rpms-toast-close');
        if (closeButton) {
            closeButton.addEventListener('click', function () {
                window.clearTimeout(timer);
                dismiss(toast);
            });
        }
    }

    function fillBody(body, messages) {
        body.textContent = '';

        if (messages.length === 1) {
            body.textContent = messages[0];
            return;
        }

        var lead = document.createElement('div');
        lead.className = 'rpms-toast-lead';
        lead.textContent = 'Please check the following:';
        body.appendChild(lead);

        var list = document.createElement('ul');
        list.className = 'rpms-toast-list';
        messages.forEach(function (message) {
            var item = document.createElement('li');
            item.textContent = message;      // textContent, never innerHTML: these strings can
            list.appendChild(item);          // carry back whatever the user typed into a field
        });
        body.appendChild(list);
    }

    function show(kind, messages) {
        var container = document.querySelector('.rpms-toasts');
        var template = document.querySelector('.rpms-toast-template[data-toast-kind="' + kind + '"]');
        if (!container || !template) return null;

        if (!Array.isArray(messages)) messages = [messages];
        messages = messages.filter(function (m) { return m; });
        if (!messages.length) return null;

        var toast = template.content.firstElementChild.cloneNode(true);
        fillBody(toast.querySelector('.rpms-toast-body'), messages);

        container.appendChild(toast);
        activate(toast);
        return toast;
    }

    // The ones the server already rendered into the page.
    Array.prototype.forEach.call(document.querySelectorAll('.rpms-toasts .rpms-toast'), activate);

    return show;
})();

// Client-side validation failures get the same treatment as anything the server sends back.
(function () {
    var current = null;

    // Submitting again re-reports the same problems, so replace the previous toast instead of
    // stacking identical copies down the screen.
    function report(messages) {
        if (current && current.isConnected) current.remove();
        current = window.rpmsToast('error', messages);
    }

    // jQuery Validate (loaded wherever unobtrusive validation is in play). It marks its forms
    // `novalidate`, so the native path below never doubles up on them.
    //
    // Bound on each form rather than delegated: jQuery Validate raises this with
    // triggerHandler(), which doesn't bubble, so a handler on document would never run.
    if (window.jQuery && window.jQuery.validator) {
        window.jQuery(function ($) {
            $('form').on('invalid-form.validate', function (event, validator) {
                report(validator.errorList.map(function (e) { return e.message; }));
            });
        });
    }

    // Native constraint validation, for controls that carry a plain `required`.
    var pending = [];
    document.addEventListener('invalid', function (event) {
        var field = event.target;
        if (!field.validationMessage) return;

        // One toast for the whole submit, not one per field: `invalid` fires per control.
        if (!pending.length) {
            window.setTimeout(function () {
                report(pending);
                pending = [];
            }, 0);
        }

        var label = field.dataset.toastLabel || field.getAttribute('aria-label') || '';
        var message = label ? label + ': ' + field.validationMessage : field.validationMessage;
        if (pending.indexOf(message) === -1) pending.push(message);
    }, true);   // capture: `invalid` doesn't bubble
})();
