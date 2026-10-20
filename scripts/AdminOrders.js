var AdminOrdersJs = (function () {

    var ttimer = null;
    var dirty = false;
    var askBox = null, askDone = null, askCheck = null, askBack = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function checked(id) {
        var e = el(id);
        return e && e.checked ? '1' : '0';
    }

    function basePath() {
        var p = location.pathname;
        return p.substring(0, p.lastIndexOf('/') + 1);
    }

    function getCookie() {
        var m = document.cookie.match(/(?:^|; )bk_tok=([0-9a-f]+)/);
        return m ? m[1] : '';
    }

    function api(method, list) {
        list = list || [];
        list.push({ key: 'tok', vlu: getCookie() });
        $ApiRequest('AdminOrders/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'bk_tok=; path=' + basePath() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signOut() {
        $ApiRequest('AdminLogin/SignOut', JSON.stringify([{ key: 'tok', vlu: getCookie() }]));
    }

    function toast(msg) {
        var t = el('ao-toast');
        t.textContent = msg;
        t.classList.add('ao-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ao-open');
        }, 3200);
    }

    function openNav() {
        el('ao-rail').classList.add('ao-open');
        el('ao-scrim').classList.add('ao-open');
    }

    function closeNav() {
        el('ao-rail').classList.remove('ao-open');
        el('ao-scrim').classList.remove('ao-open');
    }

    function openModal(title) {
        el('ao-mtitle').textContent = title;
        el('ao-modal').classList.add('ao-open');
        dirty = false;
        var f = el('ao-mbody').querySelector('input, select, textarea');
        if (f) {
            f.focus();
        }
    }

    function forceClose() {
        el('ao-modal').classList.remove('ao-open');
        el('ao-mbody').innerHTML = '';
        dirty = false;
    }

    function closeModal() {
        if (!el('ao-modal').classList.contains('ao-open')) {
            return;
        }
        if (dirty) {
            ask({ title: 'Leave without saving?', text: 'Your changes will be lost.', ok: 'Leave', no: 'Keep editing', danger: true }, forceClose);
            return;
        }
        forceClose();
    }

    function markDirty() {
        dirty = true;
    }

    function askParts() {
        return { h: askBox.querySelector('h2'), p: askBox.querySelector('p'), i: askBox.querySelector('input'), e: askBox.querySelector('em'), b: askBox.querySelectorAll('button') };
    }

    function askClose(ok) {
        if (!askBox || !askBox.classList.contains('ao-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('ao-open');
        askDone = null;
        askCheck = null;
        if (askBack && askBack.focus && document.body.contains(askBack)) {
            askBack.focus();
        }
        askBack = null;
        if (ok && f) {
            f(v);
        }
    }

    function askOk() {
        var x = askParts();
        var m = askCheck ? askCheck(x.i.value.trim()) : '';
        if (m) {
            x.e.textContent = m;
            x.i.focus();
            return;
        }
        askClose(true);
    }

    function askBuild() {
        askBox = document.createElement('div');
        askBox.className = 'ao-ask';
        askBox.innerHTML = '<div class="ao-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" aria-label="Reason"><em class="ao-err" role="alert"></em><div class="ao-askacts"><button type="button" class="ao-btn ao-btn-ghost"></button><button type="button" class="ao-btn"></button></div></div>';
        el('ao-modal').parentNode.appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('ao-open')) {
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                askClose(false);
            } else if (e.key === 'Enter' && e.target !== x.b[0]) {
                e.preventDefault();
                e.stopPropagation();
                askOk();
            } else if (e.key === 'Tab') {
                var f = [x.i, x.b[0], x.b[1]].filter(function (n) { return n.offsetParent !== null; });
                var at = f.indexOf(document.activeElement);
                e.preventDefault();
                f[(at + (e.shiftKey ? f.length - 1 : 1)) % f.length].focus();
            }
        }, true);
    }

    function ask(o, done) {
        if (!askBox) {
            askBuild();
        }
        var x = askParts(), input = typeof o.value === 'string';
        askBack = document.activeElement;
        askDone = done;
        askCheck = o.check || null;
        x.h.textContent = o.title || '';
        x.p.textContent = o.text || '';
        x.e.textContent = '';
        x.b[0].textContent = o.no || 'Cancel';
        x.b[1].textContent = o.ok || 'OK';
        x.b[1].className = 'ao-btn' + (o.danger ? ' ao-askdel' : '');
        askBox.firstChild.classList.toggle('ao-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('ao-open');
        if (input) {
            x.i.focus();
        } else {
            x.b[o.danger ? 0 : 1].focus();
        }
    }

    function debounce(fn, ms) {
        var t = null;
        return function () {
            var a = arguments;
            clearTimeout(t);
            t = setTimeout(function () {
                fn.apply(null, a);
            }, ms);
        };
    }

    var pageNo = 1;

    function filters() {
        return [
            { key: 'term', vlu: val('ao-term') },
            { key: 'status', vlu: val('ao-status') },
            { key: 'day', vlu: val('ao-day') },
            { key: 'mode', vlu: val('ao-mode') },
            { key: 'page', vlu: String(pageNo) }
        ];
    }

    function keep() {
        return [
            { key: 'fterm', vlu: val('ao-term') },
            { key: 'fstatus', vlu: val('ao-status') },
            { key: 'fday', vlu: val('ao-day') },
            { key: 'fmode', vlu: val('ao-mode') },
            { key: 'page', vlu: String(pageNo) }
        ];
    }

    function list() {
        api('List', filters());
    }

    function refresh() {
        list();
    }

    function changed() {
        pageNo = 1;
        list();
    }

    function go(n) {
        pageNo = n;
        list();
    }

    function view(id) {
        api('View', [{ key: 'id', vlu: id }]);
    }

    function advance(id, status, title, text) {
        ask({ title: title, text: text, ok: 'Yes, do it', no: 'Not yet' }, function () {
            api('SetStatus', keep().concat([{ key: 'id', vlu: id }, { key: 'status', vlu: status }]));
        });
    }

    function cancel(id, name) {
        ask({ title: 'Cancel order ' + name + '?', text: 'The customer sees it as cancelled and the time slot becomes free again.', value: '', place: 'Reason (optional)', ok: 'Cancel order', no: 'Keep it', danger: true }, function (why) {
            api('Cancel', keep().concat([{ key: 'id', vlu: id }, { key: 'why', vlu: why }]));
        });
    }

    function done(message) {
        forceClose();
        toast(message);
    }

    function start() {
        var q = new URLSearchParams(location.search).get('f') || '';
        var status = '', day = '';
        if (q === 'today' || q === 'tomorrow') {
            day = q;
        } else if (q) {
            status = q;
        }
        api('Load', [{ key: 'status', vlu: status }, { key: 'day', vlu: day }]);
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !(askBox && askBox.classList.contains('ao-open'))) {
                closeNav();
                closeModal();
            }
        });
        el('ao-modal').addEventListener('mousedown', function (e) {
            if (e.target === el('ao-modal')) {
                closeModal();
            }
        });
        el('ao-mbody').addEventListener('input', markDirty);
        el('ao-mbody').addEventListener('change', markDirty);
        el('ao-main').addEventListener('input', debounce(function (e) {
            if (e.target && e.target.id === 'ao-term') {
                changed();
            }
        }, 250));

        start();
    }

    return {
        reveal: reveal,
        toast: toast,
        expired: expired,
        signOut: signOut,
        openNav: openNav,
        closeNav: closeNav,
        openModal: openModal,
        closeModal: closeModal,
        forceClose: forceClose,
        api: api,
        refresh: refresh,
        changed: changed,
        go: go,
        view: view,
        advance: advance,
        cancel: cancel,
        done: done
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminOrdersJs.reveal);
} else {
    AdminOrdersJs.reveal();
}
