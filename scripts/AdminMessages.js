var AdminMessagesJs = (function () {

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
        $ApiRequest('AdminMessages/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'bk_tok=; path=' + basePath() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signOut() {
        $ApiRequest('AdminLogin/SignOut', JSON.stringify([{ key: 'tok', vlu: getCookie() }]));
    }

    function toast(msg) {
        var t = el('am-toast');
        t.textContent = msg;
        t.classList.add('am-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('am-open');
        }, 3200);
    }

    function openNav() {
        el('am-rail').classList.add('am-open');
        el('am-scrim').classList.add('am-open');
    }

    function closeNav() {
        el('am-rail').classList.remove('am-open');
        el('am-scrim').classList.remove('am-open');
    }

    function openModal(title) {
        el('am-mtitle').textContent = title;
        el('am-modal').classList.add('am-open');
        dirty = false;
        var f = el('am-mbody').querySelector('input, select, textarea');
        if (f) {
            f.focus();
        }
    }

    function forceClose() {
        el('am-modal').classList.remove('am-open');
        el('am-mbody').innerHTML = '';
        dirty = false;
    }

    function closeModal() {
        if (!el('am-modal').classList.contains('am-open')) {
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
        if (!askBox || !askBox.classList.contains('am-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('am-open');
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
        askBox.className = 'am-ask';
        askBox.innerHTML = '<div class="am-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" aria-label="Reason"><em class="am-err" role="alert"></em><div class="am-askacts"><button type="button" class="am-btn am-btn-ghost"></button><button type="button" class="am-btn"></button></div></div>';
        el('am-modal').parentNode.appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('am-open')) {
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
        x.b[1].className = 'am-btn' + (o.danger ? ' am-askdel' : '');
        askBox.firstChild.classList.toggle('am-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('am-open');
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

    function keep() {
        return [
            { key: 'fterm', vlu: val('am-term') },
            { key: 'fshow', vlu: val('am-show') },
            { key: 'page', vlu: String(pageNo) }
        ];
    }

    function list() {
        api('List', [
            { key: 'term', vlu: val('am-term') },
            { key: 'show', vlu: val('am-show') },
            { key: 'page', vlu: String(pageNo) }
        ]);
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

    function mark(id, read) {
        api('Mark', keep().concat([{ key: 'id', vlu: id }, { key: 'read', vlu: read ? '1' : '0' }]));
    }

    function remove(id, name) {
        ask({ title: 'Delete the message from ' + name + '?', text: 'This removes it for everyone and cannot be undone.', ok: 'Delete message', no: 'Keep it', danger: true }, function () {
            api('Delete', keep().concat([{ key: 'id', vlu: id }]));
        });
    }

    function done(message) {
        forceClose();
        toast(message);
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !(askBox && askBox.classList.contains('am-open'))) {
                closeNav();
                closeModal();
            }
        });
        el('am-modal').addEventListener('mousedown', function (e) {
            if (e.target === el('am-modal')) {
                closeModal();
            }
        });
        el('am-mbody').addEventListener('input', markDirty);
        el('am-mbody').addEventListener('change', markDirty);
        el('am-main').addEventListener('input', debounce(function (e) {
            if (e.target && e.target.id === 'am-term') {
                changed();
            }
        }, 250));

        api('Load');
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
        changed: changed,
        go: go,
        view: view,
        mark: mark,
        remove: remove,
        done: done
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminMessagesJs.reveal);
} else {
    AdminMessagesJs.reveal();
}
