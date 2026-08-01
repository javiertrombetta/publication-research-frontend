// Sidebar collapse/expand, toggled by the navbar burger button.
//
// The remembered state is applied by a small inline script in <head>, which adds
// .rpms-sidebar-is-collapsed to <html> before the page paints. This file only takes over from
// there: it moves the state onto the sidebar itself and handles clicks. Restoring the state
// here instead would mean painting an expanded sidebar first and animating it shut on every
// navigation.
(function () {
    var STORAGE_KEY = 'rpms.sidebar.collapsed';
    var PREPAINT_CLASS = 'rpms-sidebar-is-collapsed';

    var root = document.documentElement;

    // Let the first paint finish before anything is allowed to animate. Two frames, because one
    // only guarantees the style has been computed, not that it has been drawn.
    //
    // Backed by a timer as well: requestAnimationFrame does not run in a hidden tab, and without
    // the fallback a page opened in a background tab would come back with every transition and
    // animation permanently switched off.
    function allowTransitions() {
        var released = false;

        function release() {
            if (released) return;
            released = true;
            root.classList.remove('rpms-no-transitions');
        }

        requestAnimationFrame(function () {
            requestAnimationFrame(release);
        });

        setTimeout(release, 100);
    }

    var toggle = document.getElementById('sidebar-toggle');
    var sidebar = document.getElementById('rpms-sidebar');

    if (!toggle || !sidebar) {
        // Pages without a sidebar still need the guard lifted, or nothing on them animates.
        allowTransitions();
        return;
    }

    // Kept in step with the same query in site.css and in the pre-paint script in _Layout.
    var narrow = window.matchMedia('(max-width: 767.98px)');

    function isNarrow() {
        return narrow.matches;
    }

    function apply(collapsed) {
        sidebar.classList.toggle('rpms-sidebar-collapsed', collapsed);
        // Also drives the burger's morph into an X (see .rpms-burger-* in site.css).
        toggle.setAttribute('aria-expanded', String(!collapsed));
    }

    // Hand the state over from <html> to the sidebar. Both selectors collapse it, so this swap is
    // invisible, but from here on the element owns its own state.
    var collapsed = root.classList.contains(PREPAINT_CLASS);
    apply(collapsed);
    root.classList.remove(PREPAINT_CLASS);

    allowTransitions();

    function setCollapsed(nowCollapsed) {
        apply(nowCollapsed);

        // Only the desktop choice is remembered. On a phone the panel covers the page, so
        // "leave it open" is not a preference anyone holds across navigations.
        if (isNarrow()) return;

        try {
            localStorage.setItem(STORAGE_KEY, String(nowCollapsed));
        } catch (e) {
            // Private browsing: the choice just won't survive the next navigation.
        }
    }

    toggle.addEventListener('click', function () {
        setCollapsed(!sidebar.classList.contains('rpms-sidebar-collapsed'));
    });

    // Crossing the breakpoint, a rotation or a window being dragged narrower, changes what the
    // sidebar is. Going narrow it becomes a panel over the content, and an open one would be
    // covering a page the reader was in the middle of, so it shuts. Coming back to a wide screen it
    // is a column again, and the remembered preference applies.
    function onBreakpointChange() {
        if (isNarrow()) {
            apply(true);
            return;
        }

        var remembered = false;
        try {
            remembered = localStorage.getItem(STORAGE_KEY) === 'true';
        } catch (e) { /* as above */ }

        apply(remembered);
    }

    if (narrow.addEventListener) {
        narrow.addEventListener('change', onBreakpointChange);
    } else if (narrow.addListener) {
        narrow.addListener(onBreakpointChange);      // Safari before 14
    }

    // On a narrow screen the panel sits over the page, so anything outside it is a request to get
    // back to the page. On a wide screen the sidebar takes space of its own and closing it on any
    // stray click would be closing a menu nobody opened.
    document.addEventListener('click', function (event) {
        if (!isNarrow()) return;
        if (sidebar.classList.contains('rpms-sidebar-collapsed')) return;
        if (sidebar.contains(event.target) || toggle.contains(event.target)) return;

        apply(true);
    });

    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        if (!isNarrow() || sidebar.classList.contains('rpms-sidebar-collapsed')) return;

        apply(true);
        toggle.focus();
    });
})();

// The black pill behind the open sidebar item.
//
// It exists because a background painted on the active link can only appear and disappear: click
// another item and the old fill vanishes, then nothing is marked until the next page has loaded and
// painted. One element for the whole list can travel instead. It sets off the moment the click
// lands, so the menu has already answered by the time the page arrives.
(function () {
    var nav = document.querySelector('.rpms-nav');
    if (!nav) return;

    var marker = nav.querySelector('.rpms-nav-marker');
    var links = nav.querySelectorAll('.nav-link');
    if (!marker || !links.length) return;

    // Which item the marker is on. Starts as the one the server marked, and moves to whatever is
    // clicked next. The click is the answer, not the navigation that follows it.
    var current = nav.querySelector('.nav-link.active') || null;

    function place(link, instant) {
        if (!link) {
            marker.classList.remove('rpms-nav-marker-visible');
            return;
        }

        if (instant) marker.classList.add('rpms-nav-marker-instant');

        marker.style.height = link.offsetHeight + 'px';
        marker.style.transform = 'translateY(' + link.offsetTop + 'px)';
        marker.classList.add('rpms-nav-marker-visible');

        if (instant) {
            // Forces the placement to be committed before transitions come back, so the first
            // real move animates from where the marker is rather than from the top of the list.
            void marker.offsetHeight;
            marker.classList.remove('rpms-nav-marker-instant');
        }
    }

    // Only once the marker is actually placed does the outline fallback come off.
    place(current, true);
    nav.classList.add('rpms-nav-marker-ready');

    Array.prototype.forEach.call(links, function (link) {
        link.addEventListener('click', function () {
            if (link === current) return;

            // The label under the marker turns white as the marker arrives, and the one it is
            // leaving goes back to grey. Both have to happen here rather than waiting for the new
            // page: the marker is already moving, so an item left with its white text would be
            // white on white for as long as the next page takes to arrive. `active` goes too. It is
            // the server's answer to the same question, and it is now out of date.
            if (current) current.classList.remove('rpms-nav-link-selected', 'active');
            link.classList.add('rpms-nav-link-selected');

            current = link;
            place(link, false);
        });
    });

    // The list can change height without the page reloading: the sidebar opening on a phone, or a
    // label wrapping as the window narrows. The marker is placed in pixels, so it has to be
    // measured again, and this is not a movement anyone made.
    var reflow;
    window.addEventListener('resize', function () {
        window.clearTimeout(reflow);
        reflow = window.setTimeout(function () { place(current, true); }, 120);
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

        // Appearing is CSS's job (see .rpms-toast). This only schedules the exit.
        var delay = parseInt(toast.dataset.toastDelay, 10) || DEFAULT_DELAY;
        var timer = null;

        function startTimer() {
            // If the page opened in a background tab, hold the message until it's actually on
            // screen. Otherwise it would time out unseen.
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

// Select-all controls for a set of checkboxes.
//
// Declared in the markup rather than wired per screen: a button says which group of checkboxes it
// acts on (data-rpms-check-all="name") and optionally which part of the page to stay inside
// (data-rpms-scope="#some-id"). The scope is what lets one page have both a button for everything
// and a button per group without them fighting over the same name.
//
// The counter next to them (data-rpms-count="name") is the reason this is worth having at all: on
// a screen where everything starts ticked, the useful thing to tell somebody is not that a
// "select all" button exists but how much is about to go out.
(function () {
    function boxes(name, scope) {
        var root = scope ? document.querySelector(scope) : document;
        return root ? root.querySelectorAll('input[type="checkbox"][name="' + name + '"]') : [];
    }

    function refreshCounts() {
        document.querySelectorAll('[data-rpms-count]').forEach(function (label) {
            var name = label.getAttribute('data-rpms-count');
            var all = boxes(name, null);
            var checked = 0;
            Array.prototype.forEach.call(all, function (b) { if (b.checked) checked++; });
            label.textContent = String(checked);
        });
    }

    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-rpms-check-all], [data-rpms-check-none]');
        if (!button) return;

        var checkAll = button.hasAttribute('data-rpms-check-all');
        var name = button.getAttribute(checkAll ? 'data-rpms-check-all' : 'data-rpms-check-none');

        Array.prototype.forEach.call(boxes(name, button.getAttribute('data-rpms-scope')), function (box) {
            box.checked = checkAll;
        });

        refreshCounts();
    });

    // Any individual tick has to move the counter too, or it goes stale the moment somebody
    // adjusts the selection by hand after using a button.
    document.addEventListener('change', function (event) {
        if (event.target.matches('input[type="checkbox"][name]')) refreshCounts();
    });

    refreshCounts();
})();

// Paging a list the browser already holds.
//
// For a chooser, where the rows are checkboxes and the whole point is that a selection adds up
// across pages. Turning a page by reloading the screen would lose every tick made on the page being
// left, and "select all" could only ever mean the ten on screen. Every row is in the document; a
// page is which of them is shown.
//
// Declared in the markup: a container carries data-rpms-paged with a name and data-rpms-page-size,
// and each row carries data-rpms-page-item. With this script absent nothing is hidden, so the list
// is simply long, which is the right answer rather than a broken one.
(function () {
    document.querySelectorAll('[data-rpms-paged]').forEach(function (container) {
        var items = Array.prototype.slice.call(container.querySelectorAll('[data-rpms-page-item]'));
        var size = parseInt(container.getAttribute('data-rpms-page-size'), 10) || 10;
        if (items.length <= size) return;

        var pages = Math.ceil(items.length / size);
        var page = 1;

        var nav = document.createElement('div');
        nav.className = 'd-flex align-items-center gap-2 mt-3';
        nav.innerHTML =
            '<button type="button" class="btn btn-sm btn-outline-secondary" data-rpms-prev>Previous</button>' +
            '<span class="text-secondary small" data-rpms-page-label></span>' +
            '<button type="button" class="btn btn-sm btn-outline-secondary" data-rpms-next>Next</button>';
        container.parentNode.insertBefore(nav, container.nextSibling);

        var label = nav.querySelector('[data-rpms-page-label]');
        var previous = nav.querySelector('[data-rpms-prev]');
        var next = nav.querySelector('[data-rpms-next]');

        function show() {
            items.forEach(function (item, i) {
                item.hidden = Math.floor(i / size) + 1 !== page;
            });
            label.textContent = 'Page ' + page + ' of ' + pages;
            previous.disabled = page === 1;
            next.disabled = page === pages;
        }

        previous.addEventListener('click', function () { if (page > 1) { page--; show(); } });
        next.addEventListener('click', function () { if (page < pages) { page++; show(); } });

        show();
    });
})();
