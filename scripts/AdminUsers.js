var AdminUsersJs = (function () {

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
        $ApiRequest('AdminUsers/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'bk_tok=; path=' + basePath() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signOut() {
        $ApiRequest('AdminLogin/SignOut', JSON.stringify([{ key: 'tok', vlu: getCookie() }]));
    }

    function toast(msg) {
        var t = el('au-toast');
        t.textContent = msg;
        t.classList.add('au-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('au-open');
        }, 3200);
    }

    function openNav() {
        el('au-rail').classList.add('au-open');
        el('au-scrim').classList.add('au-open');
    }

    function closeNav() {
        el('au-rail').classList.remove('au-open');
        el('au-scrim').classList.remove('au-open');
    }

    function openModal(title) {
        el('au-mtitle').textContent = title;
        el('au-modal').classList.add('au-open');
        dirty = false;
        var f = el('au-mbody').querySelector('input, select, textarea');
        if (f) {
            f.focus();
        }
    }

    function forceClose() {
        el('au-modal').classList.remove('au-open');
        el('au-mbody').innerHTML = '';
        dirty = false;
    }

    function closeModal() {
        if (!el('au-modal').classList.contains('au-open')) {
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
        if (!askBox || !askBox.classList.contains('au-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('au-open');
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
        askBox.className = 'au-ask';
        askBox.innerHTML = '<div class="au-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" aria-label="Reason"><em class="au-err" role="alert"></em><div class="au-askacts"><button type="button" class="au-btn au-btn-ghost"></button><button type="button" class="au-btn"></button></div></div>';
        el('au-modal').parentNode.appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('au-open')) {
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
        x.b[1].className = 'au-btn' + (o.danger ? ' au-askdel' : '');
        askBox.firstChild.classList.toggle('au-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('au-open');
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

    function edit(id) {
        api('Edit', [{ key: 'id', vlu: id }]);
    }

    function perms() {
        var out = [];
        document.querySelectorAll('#au-mbody [data-area]').forEach(function (row) {
            var letters = '';
            row.querySelectorAll('input[data-right]').forEach(function (i) {
                if (i.checked) {
                    letters += i.getAttribute('data-right');
                }
            });
            out.push(row.getAttribute('data-area') + ':' + letters);
        });
        return out.join(';');
    }

    function adminToggle() {
        var on = el('au-f-admin').checked;
        el('au-permbox').hidden = on;
    }

    function link(row, right) {
        var v = row.querySelector('input[data-right="V"]');
        var e = row.querySelector('input[data-right="E"]');
        var x = row.querySelector('input[data-right="X"]');
        if (right === 'X' && x && x.checked) {
            e.checked = true;
        }
        if (right === 'E' && e.checked === false && x) {
            x.checked = false;
        }
        if ((right === 'E' || right === 'X') && (e.checked || (x && x.checked))) {
            v.checked = true;
        }
        if (right === 'V' && v.checked === false) {
            e.checked = false;
            if (x) {
                x.checked = false;
            }
        }
    }

    function tick(input) {
        link(input.closest('[data-area]'), input.getAttribute('data-right'));
    }

    function save() {
        api('Save', [
            { key: 'id', vlu: val('au-f-id') },
            { key: 'login', vlu: val('au-f-login') },
            { key: 'name', vlu: val('au-f-name') },
            { key: 'title', vlu: val('au-f-title') },
            { key: 'password', vlu: el('au-f-pass').value },
            { key: 'manager', vlu: checked('au-f-admin') },
            { key: 'active', vlu: checked('au-f-active') },
            { key: 'perms', vlu: perms() }
        ]);
        return false;
    }

    function toggle(id, name, active) {
        ask({ title: (active ? 'Switch off ' : 'Switch on ') + name + '?', text: active ? 'They are signed out now and cannot sign in until you switch them back on.' : 'They can sign in again with their current password.', ok: active ? 'Switch off' : 'Switch on', no: 'Keep as it is', danger: active }, function () {
            api('Toggle', [{ key: 'id', vlu: id }]);
        });
    }

    function saved(message) {
        forceClose();
        toast(message);
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !(askBox && askBox.classList.contains('au-open'))) {
                closeNav();
                closeModal();
            }
        });
        el('au-modal').addEventListener('mousedown', function (e) {
            if (e.target === el('au-modal')) {
                closeModal();
            }
        });
        el('au-mbody').addEventListener('input', markDirty);
        el('au-mbody').addEventListener('change', markDirty);

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
        edit: edit,
        adminToggle: adminToggle,
        tick: tick,
        save: save,
        toggle: toggle,
        saved: saved
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminUsersJs.reveal);
} else {
    AdminUsersJs.reveal();
}
