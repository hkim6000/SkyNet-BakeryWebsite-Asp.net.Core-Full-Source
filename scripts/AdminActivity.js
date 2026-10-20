var AdminActivityJs = (function () {

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
        $ApiRequest('AdminActivity/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'bk_tok=; path=' + basePath() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signOut() {
        $ApiRequest('AdminLogin/SignOut', JSON.stringify([{ key: 'tok', vlu: getCookie() }]));
    }

    function toast(msg) {
        var t = el('ay-toast');
        t.textContent = msg;
        t.classList.add('ay-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ay-open');
        }, 3200);
    }

    function openNav() {
        el('ay-rail').classList.add('ay-open');
        el('ay-scrim').classList.add('ay-open');
    }

    function closeNav() {
        el('ay-rail').classList.remove('ay-open');
        el('ay-scrim').classList.remove('ay-open');
    }

    function openModal(title) {
        el('ay-mtitle').textContent = title;
        el('ay-modal').classList.add('ay-open');
        dirty = false;
        var f = el('ay-mbody').querySelector('input, select, textarea');
        if (f) {
            f.focus();
        }
    }

    function forceClose() {
        el('ay-modal').classList.remove('ay-open');
        el('ay-mbody').innerHTML = '';
        dirty = false;
    }

    function closeModal() {
        if (!el('ay-modal').classList.contains('ay-open')) {
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
        if (!askBox || !askBox.classList.contains('ay-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('ay-open');
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
        askBox.className = 'ay-ask';
        askBox.innerHTML = '<div class="ay-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" aria-label="Reason"><em class="ay-err" role="alert"></em><div class="ay-askacts"><button type="button" class="ay-btn ay-btn-ghost"></button><button type="button" class="ay-btn"></button></div></div>';
        el('ay-modal').parentNode.appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('ay-open')) {
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
        x.b[1].className = 'ay-btn' + (o.danger ? ' ay-askdel' : '');
        askBox.firstChild.classList.toggle('ay-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('ay-open');
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

    function list() {
        api('List', [
            { key: 'term', vlu: val('ay-term') },
            { key: 'result', vlu: val('ay-result') },
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

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !(askBox && askBox.classList.contains('ay-open'))) {
                closeNav();
                closeModal();
            }
        });
        el('ay-modal').addEventListener('mousedown', function (e) {
            if (e.target === el('ay-modal')) {
                closeModal();
            }
        });
        el('ay-mbody').addEventListener('input', markDirty);
        el('ay-mbody').addEventListener('change', markDirty);
        el('ay-main').addEventListener('input', debounce(function (e) {
            if (e.target && e.target.id === 'ay-term') {
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
        go: go
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminActivityJs.reveal);
} else {
    AdminActivityJs.reveal();
}
