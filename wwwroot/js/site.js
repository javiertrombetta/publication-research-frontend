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

        // The same fact on the root as well. The panel is fixed on a wide screen, so the content
        // and the footer have to be moved over to leave room for it, and neither is a sibling of
        // the panel: there is no selector that reaches them from it.
        root.classList.toggle('rpms-sidebar-shut', collapsed);

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

    // Where the burger sits on a wide screen: halfway down the list, on the sidebar's edge.
    //
    // The list is a different height for every role, so the middle of it is a measurement rather
    // than a number a stylesheet could hold. A viewport position, and it stays one: the panel is
    // stuck under the bar, so the list is in the same place on the screen however far the page has
    // been scrolled, and so is the handle on its edge.
    var navList = sidebar.querySelector('.rpms-nav');

    function placeToggle() {
        if (!navList) return;

        var box = navList.getBoundingClientRect();
        if (!box.height) return;      // Collapsed to nothing on a phone: leave the last figure.

        root.style.setProperty('--rpms-burger-top', Math.round(box.top + box.height / 2) + 'px');
    }

    placeToggle();
    window.addEventListener('resize', placeToggle);

    // Dragging the handle drags the panel.
    //
    // The handle sits on the panel's edge, so taking hold of it and pulling is the movement the
    // shape already suggests: the panel follows the pointer, and where it is let go decides
    // whether it finishes opening or closes. A click still just toggles, which is why nothing
    // happens until the pointer has actually travelled: a few pixels of wobble while pressing a
    // button is not a drag, and treating it as one would make the button feel broken.
    var DRAG_THRESHOLD = 4;

    var dragging = false;
    var dragged = false;
    var suppressClick = false;
    var startX = 0;
    var startWidth = 0;

    function openWidth() {
        return parseFloat(getComputedStyle(root).getPropertyValue('--rpms-sidebar-width')) || 260;
    }

    toggle.addEventListener('pointerdown', function (event) {
        // Left button only, and not on a phone, where the panel comes down over the page rather
        // than out from the side and there is no edge to pull.
        if (isNarrow() || (event.pointerType === 'mouse' && event.button !== 0)) return;

        dragging = true;
        dragged = false;
        startX = event.clientX;
        startWidth = sidebar.classList.contains('rpms-sidebar-collapsed') ? 0 : openWidth();

        // So the drag survives the pointer leaving the button, which it does immediately.
        if (toggle.setPointerCapture) toggle.setPointerCapture(event.pointerId);
    });

    toggle.addEventListener('pointermove', function (event) {
        if (!dragging) return;

        var travelled = event.clientX - startX;

        if (!dragged) {
            if (Math.abs(travelled) < DRAG_THRESHOLD) return;
            dragged = true;
            root.classList.add('rpms-sidebar-dragging');
        }

        var full = openWidth();
        var width = Math.min(full, Math.max(0, startWidth + travelled));

        root.style.setProperty('--rpms-drag-width', width + 'px');
        root.style.setProperty('--rpms-drag-progress', String(width / full));
    });

    function endDrag() {
        if (!dragging) return;
        dragging = false;

        // Pressed and released without moving: an ordinary click, and the click handler has it.
        if (!dragged) return;

        // The click that follows a drag would otherwise undo the decision just made by hand.
        suppressClick = true;

        // Both measurements before anything changes. openWidth reads a computed style, which makes
        // the browser work out the current one there and then, and where that happens matters: a
        // transition starts from the last value worked out rather than from the last one drawn.
        // Between letting go of the panel and saying where it should end up, that value is the
        // width the panel has when nobody is dragging it, which is wide open. Reading it there put
        // the page back where it had been before the drag and slid it shut from scratch.
        var full = openWidth();
        var width = parseFloat(root.style.getPropertyValue('--rpms-drag-width'));

        // Let go past halfway and it opens, short of it and it shuts. Said first, and only then is
        // the drag let go of, so the panel carries on from where the hand left it.
        setCollapsed(!(width >= full / 2));
        root.classList.remove('rpms-sidebar-dragging');
    }

    toggle.addEventListener('pointerup', endDrag);
    toggle.addEventListener('pointercancel', endDrag);

    toggle.addEventListener('click', function () {
        if (suppressClick) {
            suppressClick = false;
            return;
        }

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

// A field one button insists on and the others do not.
//
// Every review screen has this shape: accepting something that is in order explains itself, and
// sending it back is nothing but the explanation. `required` on the field cannot say that, since
// it would stop both buttons, so the button carries the rule instead:
//
//     data-rpms-needs-comments="<id of the field>"
//
// The API refuses these too. This is so the person is told before losing the page and what they
// typed, rather than being sent back to the screen to type it again.
(function () {
    document.addEventListener('click', function (event) {
        var button = event.target.closest('[data-rpms-needs-comments]');
        if (!button) return;

        var field = document.getElementById(button.dataset.rpmsNeedsComments);
        if (field && field.value.trim().length > 0) return;

        event.preventDefault();
        if (field) field.focus();
        window.rpmsToast('error', [
            button.dataset.rpmsNeedsCommentsMessage ||
            'Say what needs changing. It is all the student is given to work from.'
        ]);
    });
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
    // What a cell sorts by. Usually what it says; where that is written for people to read rather
    // than to order, the cell can carry the value to use instead (data-rpms-sort-value), which is
    // how a date shown as "04 Aug 2026" sorts as a date and an empty one sorts to the end.
    function cellText(row, index) {
        var cell = row.children[index];
        if (!cell) return '';

        var explicit = cell.getAttribute('data-rpms-sort-value');
        return explicit !== null ? explicit.trim() : cell.textContent.trim();
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

    // Something else has already refused this click, a missing reason being the usual one. Asking
    // to confirm an action that is not going to happen is a question about nothing.
    if (event.defaultPrevented) return;

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


// ==========================================================
// Switching between the light and the dark theme
// ==========================================================
//
// The switch is a form, and submitting it works on its own: the server writes the cookie and the
// next page is drawn in the new theme. That is the version somebody without JavaScript gets, and
// it is correct, but it is a hard cut between two paints because a CSS transition cannot run
// across a navigation.
//
// So where JavaScript is available the attribute is flipped here instead, which the transitions in
// site.css then animate, and the form is posted in the background to record the choice. Nothing
// reloads, so the page being read stays where it is and the change is a fade rather than a blink.
(function () {
    // Every switch on the page, not the first. A signed-out header carries one and the signed-in
    // menu carries another, and a page could reasonably hold both; wiring only the first would
    // leave the other reloading while its neighbour faded.
    var forms = document.querySelectorAll('[data-rpms-theme-form]');
    if (!forms.length) return;

    Array.prototype.forEach.call(forms, function (form) {
        // The switch is an item in the user menu, and Bootstrap shuts a menu on any click inside
        // it. Reasonable for the items that take you somewhere, wrong for this one: it changes the
        // page it is sitting on and leaves you there, so the menu should stay up, whether to see
        // the change or to change it back. Kept from the document, which is where that handler is,
        // and only for this control. The button still submits: stopping a click going up is not
        // stopping what it does.
        form.addEventListener('click', function (event) {
            event.stopPropagation();
        });

        form.addEventListener('submit', function (event) {
            var wanted = form.querySelector('input[name="theme"]');
            if (!wanted) return;

            event.preventDefault();

            var root = document.documentElement;
            var next = wanted.value === 'dark' ? 'dark' : 'light';

            // The paint, immediately. Everything below is bookkeeping the reader never waits for.
            root.setAttribute('data-bs-theme', next);

            // Recorded in the background: the cookie for this browser, and the account behind it, so
            // the choice survives signing out and follows the person to another machine. A failure
            // here leaves the page correct until the next load, which is the right way for it to fail.
            var body = new FormData(form);
            body.set('theme', next);
            fetch(form.action, { method: 'POST', body: body, credentials: 'same-origin' })
                .catch(function () { /* The theme still changed. Nothing to tell anybody. */ });

            // Every switch now offers the other theme, so pressing any of them twice returns where
            // it started without a round trip. Labels and icons follow the attribute through CSS,
            // so there is nothing else here to keep in step by hand.
            Array.prototype.forEach.call(forms, function (other) {
                var field = other.querySelector('input[name="theme"]');
                if (field) field.value = next === 'dark' ? 'light' : 'dark';
            });
        });
    });
})();

// Searching as you type.
//
// Every search box on the site narrows a list the API holds, so the search has to reach the API:
// what is on screen is one page of the answer, not the answer. That means a page load per search,
// which is exactly why these all had a button. Pressed once, the reader waits; pressed after every
// keystroke, they would be interrupted mid-word.
//
// So: three characters before anything happens, and a pause after typing stops. Three because one
// or two match most of a department and the round trip buys nothing; the pause because it is the
// difference between one request and one per letter. Emptying the box searches immediately, since
// that is somebody asking for the whole list back rather than for a narrower one.
//
// The caret is put back where it was afterwards, which is what makes it read as live rather than
// as a page that keeps reloading underneath you. The button stays for anyone who prefers it, and
// for anyone without JavaScript this whole block simply does not run.
(function () {
    var DELAY = 400;
    var MINIMUM = 3;
    var RESUME_KEY = 'rpms-search-resume';

    function formFor(input) {
        // form= wins over containing form: the supervisor chooser's box sits inside the dispatch
        // form and belongs to a different one, and submitting the wrong one would send proposals.
        return input.form;
    }

    function run() {
        var inputs = document.querySelectorAll('input[type="search"]');
        if (!inputs.length) return;

        Array.prototype.forEach.call(inputs, function (input) {
            var form = formFor(input);

            // Only where a search is a link. A POST form does something, and doing it because
            // somebody paused while typing is not a thing to arrange.
            if (!form || (form.method || 'get').toLowerCase() !== 'get') return;

            var timer = null;
            var initial = input.value;

            input.addEventListener('input', function () {
                window.clearTimeout(timer);

                var value = input.value.trim();

                // Back to what the page was already showing: nothing to ask for.
                if (value === initial.trim()) return;

                if (value.length > 0 && value.length < MINIMUM) return;

                timer = window.setTimeout(function () {
                    try {
                        window.sessionStorage.setItem(RESUME_KEY, JSON.stringify({
                            path: window.location.pathname,
                            name: input.getAttribute('name')
                        }));
                    } catch (ignored) {
                        // Private browsing refuses storage. The search still runs; the caret is
                        // the only thing lost, and that is not worth abandoning the search over.
                    }

                    form.submit();
                }, DELAY);
            });
        });

        // Put the reader back in the box they were typing in, at the end of what they typed.
        var resume = null;
        try {
            resume = JSON.parse(window.sessionStorage.getItem(RESUME_KEY) || 'null');
            window.sessionStorage.removeItem(RESUME_KEY);
        } catch (ignored) { /* Nothing to restore. */ }

        if (!resume || resume.path !== window.location.pathname) return;

        var box = document.querySelector('input[type="search"][name="' + resume.name + '"]');
        if (!box) return;

        // Without preventScroll the browser jumps to wherever it decides the box is, which on a
        // page that just got shorter is not where the reader was. The box is at the top of these
        // screens anyway, so there is nothing to scroll to.
        box.focus({ preventScroll: true });
        box.setSelectionRange(box.value.length, box.value.length);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run);
    } else {
        run();
    }
})();

// Putting the sidebar's items in the order somebody wants them.
//
// The difficulty is that every item is a link, and a link answers to being pressed. So pressing is
// not what starts a move: holding is. Press and let go and you have followed the link, exactly as
// before; press and keep holding and the item lifts off the list and follows the pointer until it
// is put down. Nothing is guessed from how far the pointer travelled, which is the usual way of
// telling a click from a drag and the reason drags of a few pixels open pages nobody asked for.
//
// The order belongs to the person, not to the machine. It arrives on the page from their account
// by way of the session, and a change is posted straight back: kept in this browser instead, one
// person's arrangement was handed to whoever signed in on that browser next.
(function () {
    var HOLD_MS = 400;
    var SLIP = 8;              // How far a finger may wander before a hold is read as a scroll.

    var nav = document.querySelector('.rpms-nav');
    if (!nav) return;

    function items() {
        return Array.prototype.slice.call(nav.querySelectorAll('.nav-link'));
    }

    // The route each item opens, which is what it is. Its label changes with the role and its
    // position is the very thing being recorded, so neither would do.
    function keyOf(link) {
        return link.getAttribute('href') || '';
    }

    // Written into the markup by the server, from this person's account. Space-separated, since a
    // route has no spaces in it.
    function readOrder() {
        return (nav.getAttribute('data-rpms-nav-order') || '').split(' ').filter(Boolean);
    }

    // Posted and forgotten. The menu is already in the order it was just put in; what this records
    // is that it should still be that way tomorrow, and interrupting somebody to say a preference
    // failed to save would cost more than the preference is worth.
    //
    // After a pause, and only the last arrangement. Somebody nudging an item three places up with
    // the keyboard makes three arrangements in a moment, and sending all three is a race the
    // slowest wins: the order that stuck was whichever request the server happened to finish
    // last, which was not the one on the screen.
    var pending = null;

    function saveOrder() {
        nav.setAttribute('data-rpms-nav-order', items().map(keyOf).join(' '));

        window.clearTimeout(pending);
        pending = window.setTimeout(send, 400);
    }

    function send() {
        var url = nav.getAttribute('data-rpms-nav-order-url');
        if (!url) return;

        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var headers = { 'Content-Type': 'application/json' };
        if (token) headers['RequestVerificationToken'] = token.value;

        fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: headers,
            body: JSON.stringify(items().map(keyOf))
        }).catch(function () { /* Still arranged. Just not remembered. */ });
    }

    // A pause is no use to somebody who arranges the menu and closes the tab. sendBeacon goes out
    // whether or not the page survives long enough to hear back, which is all this needs.
    window.addEventListener('pagehide', function () {
        if (!pending) return;
        window.clearTimeout(pending);
        pending = null;

        var url = nav.getAttribute('data-rpms-nav-order-url');
        if (!url || !navigator.sendBeacon) return;

        // No header goes with a beacon, so the token travels in the body, where the framework also
        // looks for it.
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        var form = new FormData();
        form.append('items', items().map(keyOf).join(' '));
        if (token) form.append('__RequestVerificationToken', token.value);

        navigator.sendBeacon(url, form);
    });

    // Applied on load. An item nobody has an opinion about yet, because the menu has grown or this
    // person has a role they did not have before, keeps its place at the end rather than being
    // dropped or pushed to the top.
    function applyOrder() {
        var wanted = readOrder();
        if (!wanted.length) return;

        var known = items().filter(function (link) { return wanted.indexOf(keyOf(link)) !== -1; });
        known.sort(function (a, b) { return wanted.indexOf(keyOf(a)) - wanted.indexOf(keyOf(b)); });

        // Appended in order after everything else, then the strangers follow, in the order the
        // server sent them.
        var strangers = items().filter(function (link) { return wanted.indexOf(keyOf(link)) === -1; });
        known.concat(strangers).forEach(function (link) { nav.appendChild(link); });
    }

    applyOrder();

    var lifted = null;
    var pointerId = null;
    var holdTimer = null;
    var startY = 0;
    var pressY = 0;
    var pressX = 0;
    var moved = false;
    var orderBefore = null;

    function clearHold() {
        window.clearTimeout(holdTimer);
        holdTimer = null;
    }

    function lift(link) {
        lifted = link;
        orderBefore = items().map(keyOf);

        nav.classList.add('rpms-nav-reordering');
        link.classList.add('rpms-nav-link-lifted');

        // The list is being rearranged, not scrolled, and not selected either.
        if (link.setPointerCapture && pointerId !== null) {
            try { link.setPointerCapture(pointerId); } catch (e) { /* gone already */ }
        }
    }

    function place(clientY) {
        var offset = clientY - startY;
        lifted.style.transform = 'translateY(' + offset + 'px)';

        var box = lifted.getBoundingClientRect();
        var middle = box.top + box.height / 2;

        var others = items().filter(function (link) { return link !== lifted; });

        for (var i = 0; i < others.length; i++) {
            var other = others[i];
            var rect = other.getBoundingClientRect();
            if (middle < rect.top || middle > rect.bottom) continue;

            var was = lifted.offsetTop;

            if (middle < rect.top + rect.height / 2) {
                nav.insertBefore(lifted, other);
            } else {
                nav.insertBefore(lifted, other.nextSibling);
            }

            // The item has just been given a different place in the list, so its untransformed
            // position has moved. Move the reference point with it, or it would jump out from
            // under the pointer by exactly the height of whatever it stepped over.
            startY += lifted.offsetTop - was;
            lifted.style.transform = 'translateY(' + (clientY - startY) + 'px)';
            break;
        }
    }

    function drop(keep) {
        if (!lifted) return;

        if (!keep && orderBefore) {
            var by = {};
            items().forEach(function (link) { by[keyOf(link)] = link; });
            orderBefore.forEach(function (key) { if (by[key]) nav.appendChild(by[key]); });
        }

        lifted.style.transform = '';
        lifted.classList.remove('rpms-nav-link-lifted');
        nav.classList.remove('rpms-nav-reordering');

        if (keep) saveOrder();

        lifted = null;
        pointerId = null;
        orderBefore = null;
    }

    nav.addEventListener('pointerdown', function (event) {
        var link = event.target.closest ? event.target.closest('.nav-link') : null;
        if (!link || !nav.contains(link)) return;
        if (event.pointerType === 'mouse' && event.button !== 0) return;

        pointerId = event.pointerId;
        pressX = event.clientX;
        pressY = event.clientY;
        moved = false;

        clearHold();
        holdTimer = window.setTimeout(function () {
            holdTimer = null;
            startY = pressY;
            lift(link);
        }, HOLD_MS);
    });

    nav.addEventListener('pointermove', function (event) {
        if (lifted) {
            moved = true;
            event.preventDefault();
            place(event.clientY);
            return;
        }

        // Still waiting on the hold. A finger that has set off somewhere is scrolling the menu,
        // and a mouse that has wandered is not holding still, so neither is a request to rearrange.
        if (!holdTimer) return;
        if (Math.abs(event.clientX - pressX) > SLIP || Math.abs(event.clientY - pressY) > SLIP) {
            clearHold();
        }
    });

    function release() {
        clearHold();
        if (lifted) drop(true);
    }

    nav.addEventListener('pointerup', release);
    nav.addEventListener('pointercancel', function () {
        clearHold();
        if (lifted) drop(false);
    });

    // A link that has been carried somewhere must not also be followed. The click arrives after
    // the pointer is up, which is where this catches it.
    nav.addEventListener('click', function (event) {
        if (!moved) return;
        moved = false;
        event.preventDefault();
        event.stopPropagation();
    }, true);

    // Holding a link on a touch screen otherwise offers to copy it, and dragging one offers to
    // drag the address somewhere.
    nav.addEventListener('contextmenu', function (event) {
        if (lifted) event.preventDefault();
    });

    nav.addEventListener('dragstart', function (event) { event.preventDefault(); });

    // The same thing from the keyboard, for anybody not using a pointer: hold Alt and use the
    // arrows on the item that has focus.
    nav.addEventListener('keydown', function (event) {
        if (!event.altKey) return;
        if (event.key !== 'ArrowUp' && event.key !== 'ArrowDown') return;

        var link = event.target.closest ? event.target.closest('.nav-link') : null;
        if (!link) return;

        var list = items();
        var at = list.indexOf(link);
        var to = event.key === 'ArrowUp' ? at - 1 : at + 1;
        if (to < 0 || to >= list.length) return;

        event.preventDefault();

        if (event.key === 'ArrowUp') nav.insertBefore(link, list[to]);
        else nav.insertBefore(link, list[to].nextSibling);

        saveOrder();
        link.focus();
    });

    // Somebody who changes their mind mid-carry gets the list back as it was.
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && lifted) drop(false);
    });
})();
