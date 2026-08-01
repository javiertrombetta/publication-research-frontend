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

// The pill sliding between sidebar items.
//
// The resting state is not this script's job. CSS paints the open item from the first frame, using
// the class the server already put there, so a page that has just loaded looks right before any
// JavaScript runs. This used to draw that state as well, which meant the sidebar wore one look
// until the script had measured it and another afterwards; every click here is a page load, so
// every click flickered.
//
// What is left is the part CSS cannot do: when somebody clicks a different item, the fill travels
// there rather than vanishing from one place and reappearing in another once the next page has
// arrived. The pill is invisible and idle until then.
(function () {
    var nav = document.querySelector('.rpms-nav');
    if (!nav) return;

    var marker = nav.querySelector('.rpms-nav-marker');
    var links = nav.querySelectorAll('.nav-link');
    if (!marker || !links.length) return;

    // Which item is open. The server's answer to start with, and whatever is clicked after that.
    var current = nav.querySelector('.nav-link.active') || null;

    function moveTo(link, instant) {
        if (instant) marker.classList.add('rpms-nav-marker-instant');

        marker.style.height = link.offsetHeight + 'px';
        marker.style.transform = 'translateY(' + link.offsetTop + 'px)';

        if (instant) {
            // Commits the placement before transitions come back, so the journey starts from where
            // the marker is rather than from the top of the list.
            void marker.offsetHeight;
            marker.classList.remove('rpms-nav-marker-instant');
        }
    }

    Array.prototype.forEach.call(links, function (link) {
        link.addEventListener('click', function () {
            if (link === current || !current) return;

            // Hand the fill over to the marker: put it exactly where the open item's own background
            // is, with no animation, make it visible, and only then let the links drop theirs. Done
            // in that order there is nothing to see at the swap.
            moveTo(current, true);
            marker.classList.add('rpms-nav-marker-visible');
            nav.classList.add('rpms-nav-travelling');

            // The label the marker is arriving at turns white, the one it is leaving goes back to
            // grey. Both now rather than on the next page: the marker is already moving, and an
            // item left white would be white on white until the page arrived. `active` goes with
            // it, being the server's answer to a question that has just changed.
            current.classList.remove('rpms-nav-link-selected', 'active');
            link.classList.add('rpms-nav-link-selected');

            current = link;
            moveTo(link, false);
        });
    });

    // The list can change height without the page reloading: the sidebar opening on a phone, or a
    // label wrapping as the window narrows. Only worth re-measuring while the marker is on screen,
    // which is to say mid-journey.
    var reflow;
    window.addEventListener('resize', function () {
        if (!nav.classList.contains('rpms-nav-travelling')) return;
        window.clearTimeout(reflow);
        reflow = window.setTimeout(function () { moveTo(current, true); }, 120);
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

    // Ticking only what has already been out once and come back.
    //
    // Its own button rather than a filter on the list, because the two are worked on together: a
    // coordinator who has just read that eight proposals found nobody wants those eight ticked and
    // sent to different supervisors, without hunting for them among the ones nobody has seen yet.
    // Everything else is untucked, so what is about to go out is exactly what the button says.
    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-rpms-check-returned]');
        if (!button) return;

        var name = button.getAttribute('data-rpms-check-returned');

        Array.prototype.forEach.call(boxes(name, button.getAttribute('data-rpms-scope')), function (box) {
            box.checked = box.getAttribute('data-rpms-returned') === 'true';
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

// Sorting a small table that is entirely on the page.
//
// For the lists nested inside a card: the ethics documents on a publication, the reviews on a
// paper. They are short, complete, and never paged, so the rows to sort are the rows there are and
// a round trip would buy nothing. This is the opposite case from a queue, where the row somebody
// wants is on another page and only the database can order it.
//
// Declared in the markup: a table carries data-rpms-sortable-table, and each heading that can be
// sorted by carries data-rpms-sort-column with the cell index. Headings become buttons; a second
// click reverses. Without this script the table is a table, which is what it was before.
(function () {
    function cellText(row, index) {
        var cell = row.children[index];
        return cell ? cell.textContent.trim() : '';
    }

    // Dates and numbers should not sort as text: "9" after "10", "1 Feb" before "1 Jan".
    function comparable(text) {
        var date = Date.parse(text);
        if (!isNaN(date)) return date;

        var number = parseFloat(text.replace(/[^0-9.-]/g, ''));
        if (text !== '' && !isNaN(number) && /^[^a-zA-Z]*$/.test(text)) return number;

        return text.toLowerCase();
    }

    document.querySelectorAll('[data-rpms-sortable-table]').forEach(function (table) {
        var body = table.tBodies[0];
        if (!body || body.rows.length < 2) return;

        table.querySelectorAll('[data-rpms-sort-column]').forEach(function (heading) {
            var index = parseInt(heading.getAttribute('data-rpms-sort-column'), 10);
            // Only the clickable class. .rpms-sort is display: inline-flex, and putting that on a
            // <th> stops it being a table cell: the headings left their columns and bunched up at
            // the left of the row while the body below stayed where it was.
            heading.classList.add('rpms-sort-clickable');
            heading.setAttribute('role', 'button');
            heading.setAttribute('tabindex', '0');

            function sort() {
                var descending = heading.getAttribute('data-rpms-sorted') === 'asc';

                table.querySelectorAll('[data-rpms-sort-column]').forEach(function (other) {
                    other.removeAttribute('data-rpms-sorted');
                });

                heading.setAttribute('data-rpms-sorted', descending ? 'desc' : 'asc');

                Array.prototype.slice.call(body.rows)
                    .sort(function (a, b) {
                        var left = comparable(cellText(a, index));
                        var right = comparable(cellText(b, index));
                        if (left < right) return descending ? 1 : -1;
                        if (left > right) return descending ? -1 : 1;
                        return 0;
                    })
                    .forEach(function (row) { body.appendChild(row); });
            }

            heading.addEventListener('click', sort);
            heading.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); sort(); }
            });
        });
    });
})();

// Opening and closing every group at once.
//
// A queue grouped by student is comfortable when there are three groups and a scroll when there
// are thirty. One control for the lot lets somebody shut everything and open only what they are
// working on. Declared in the markup: the panels carry data-rpms-collapsible with a group name,
// the buttons carry data-rpms-expand-all or data-rpms-collapse-all with the same name.
//
// Driven through Bootstrap's own Collapse where it is available, so the per-group toggles and
// these buttons agree about what is open. Falling back to the class directly keeps it working if
// the bundle ever stops exposing the API, which it has done before.
(function () {
    function panels(name) {
        return document.querySelectorAll('[data-rpms-collapsible="' + name + '"]');
    }

    function set(panel, open) {
        var api = window.bootstrap && window.bootstrap.Collapse
            ? window.bootstrap.Collapse.getOrCreateInstance(panel, { toggle: false })
            : null;

        if (api) {
            if (open) api.show(); else api.hide();
        } else {
            panel.classList.toggle('show', open);
        }

        // The arrow is drawn off the header's aria-expanded, so the header has to be told as well.
        // Both spellings, because a panel may be driven by a header or by a control of its own.
        var toggles = document.querySelectorAll(
            '[data-rpms-header-toggle="#' + panel.id + '"], [data-bs-target="#' + panel.id + '"]');
        Array.prototype.forEach.call(toggles, function (toggle) {
            toggle.setAttribute('aria-expanded', String(open));
        });
    }

    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-rpms-expand-all], [data-rpms-collapse-all]');
        if (!button) return;

        var open = button.hasAttribute('data-rpms-expand-all');
        var name = button.getAttribute(open ? 'data-rpms-expand-all' : 'data-rpms-collapse-all');

        panels(name).forEach(function (panel) { set(panel, open); });
    });

    // The header is the control, all of it.
    //
    // The chevron is a 20px target, and the thing beside it that everybody actually aims at is the
    // student's name. It used to be a button of its own, which made it the only way in and left it
    // sitting there focused and pressed-looking after a click. Now it is only an arrow, drawn
    // inside a header that carries the role, the focus and the keyboard handling.
    //
    // Anything in the header that does something else keeps doing it: a tickbox, a link, a button.
    // Clicks landing on those are left alone rather than also opening the card.
    function toggleFrom(header) {
        var panel = document.querySelector(header.getAttribute('data-rpms-header-toggle'));
        if (panel) set(panel, !panel.classList.contains('show'));
    }

    document.addEventListener('click', function (event) {
        var header = event.target.closest('[data-rpms-header-toggle]');
        if (!header) return;
        if (event.target.closest('a, button, input, label, select, textarea')) return;

        toggleFrom(header);
    });

    // What a real button would have given for free. Space scrolls the page by default, so it is
    // the one that has to be stopped.
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Enter' && event.key !== ' ' && event.key !== 'Spacebar') return;

        var header = event.target.closest('[data-rpms-header-toggle]');
        if (!header || header !== event.target) return;

        event.preventDefault();
        toggleFrom(header);
    });
})();

// One question before something that cannot be undone.
//
// Declared on the control itself, so the wording is written beside the thing it is asking about
// rather than in a table of messages somewhere else. The default action is only stopped when the
// answer is no, so a control without this attribute behaves exactly as it always did.
document.addEventListener('click', function (event) {
    var control = event.target.closest('[data-rpms-confirm]');
    if (!control) return;

    if (!window.confirm(control.getAttribute('data-rpms-confirm'))) {
        event.preventDefault();
        event.stopPropagation();
    }
});

// A form that borrows the reason typed into a box belonging to another one.
//
// Two buttons, one decision, one place to write down why. The assign form owns the comments box
// because that is where it reads; the form behind the other button copies the value across as it
// goes, rather than making somebody type the same sentence into a second box to say no.
document.addEventListener('submit', function (event) {
    var source = event.target.getAttribute && event.target.getAttribute('data-rpms-comments-from');
    if (!source) return;

    var from = document.querySelector(source);
    var into = event.target.querySelector('input[name="comments"]');
    if (from && into) into.value = from.value;
});

// Only the chosen storage destination's own settings.
//
// The four destinations want different things and only one of them is in use, so showing all four
// sets at once would be four times the form for no reason, and a bucket name sitting next to a
// directory path invites somebody to fill in both and wonder which won.
//
// Without JavaScript every panel stays visible, which is the honest fallback: the form still works
// and only the fields belonging to the chosen destination are read on the server.
(function () {
    var chooser = document.querySelector('[data-rpms-storage-provider]');
    if (!chooser) return;

    var panels = document.querySelectorAll('[data-rpms-storage-panel]');

    function show() {
        Array.prototype.forEach.call(panels, function (panel) {
            panel.hidden = panel.getAttribute('data-rpms-storage-panel') !== chooser.value;
        });
    }

    chooser.addEventListener('change', show);
    show();
})();
