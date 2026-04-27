/* ==========================================================================
   AvivaNext — site.js
   Shared JavaScript for all pages
   ========================================================================== */

// --------------------------------------------------------------------------
// Login form: hide button, show spinner on submit
// --------------------------------------------------------------------------
(function initLoginForm() {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('loginForm');
        if (!form) return;
        form.addEventListener('submit', function () {
            var btn = document.getElementById('loginBtn');
            var loading = document.getElementById('loading');
            if (btn) btn.style.display = 'none';
            if (loading) {
                loading.classList.remove('hidden');
                loading.style.display = 'block';
            }
        });
    });
}());

// --------------------------------------------------------------------------
// Setup form: log submit (extend as needed)
// --------------------------------------------------------------------------
(function initSetupForm() {
    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('setupForm');
        if (!form) return;
        form.addEventListener('submit', function () {
            console.log('Setup form submit event triggered');
        });
    });
}());

// --------------------------------------------------------------------------
// Attendance table: client-side search/filter
// --------------------------------------------------------------------------
function filterTable(query) {
    var rows = document.querySelectorAll('#tableBody tr');
    var q = query.toLowerCase();
    rows.forEach(function (row) {
        var text = row.textContent.toLowerCase();
        row.style.display = text.includes(q) ? '' : 'none';
    });
}

// --------------------------------------------------------------------------
// Dashboard helpers
// --------------------------------------------------------------------------

/**
 * Set text content on an element and strip skeleton class.
 * @param {string} id
 * @param {string|number} val
 */
function setText(id, val) {
    var el = document.getElementById(id);
    if (!el) return;
    el.textContent = val;
    el.classList.remove('skeleton');
    el.style.width = '';
    el.style.height = '';
}

/**
 * Return a human-readable "time ago" string.
 * @param {string|Date} dateStr
 * @returns {string}
 */
function timeAgo(dateStr) {
    var diff = Math.floor((Date.now() - new Date(dateStr)) / 1000);
    if (diff < 60) return diff + 's ago';
    if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
    if (diff < 86400) return Math.floor(diff / 3600) + 'h ago';
    return new Date(dateStr).toLocaleDateString();
}

/**
 * Format seconds as "Xh Ym".
 * @param {number} seconds
 * @returns {string}
 */
function formatHours(seconds) {
    var h = Math.floor(seconds / 3600);
    var m = Math.floor((seconds % 3600) / 60);
    return h + 'h ' + m + 'm';
}

// --------------------------------------------------------------------------
// Viewport height fix for mobile browsers (address-bar resize bug)
// Sets --vh CSS variable that equals 1% of the true visible height.
// Usage in CSS: height: calc(var(--vh, 1vh) * 100)
// --------------------------------------------------------------------------
(function initViewportHeightFix() {
    function setVh() {
        var vh = window.innerHeight * 0.01;
        document.documentElement.style.setProperty('--vh', vh + 'px');
    }

    setVh();

    var resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(setVh, 100);
    });

    // Also re-run after orientation change settles
    window.addEventListener('orientationchange', function () {
        setTimeout(setVh, 300);
    });
}());
