var AdminProductsJs = (function () {

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
        $ApiRequest('AdminProducts/' + method, JSON.stringify(list));
    }

    function expired() {
        document.cookie = 'bk_tok=; path=' + basePath() + '; max-age=0';
        location.href = 'AdminLogin';
    }

    function signOut() {
        $ApiRequest('AdminLogin/SignOut', JSON.stringify([{ key: 'tok', vlu: getCookie() }]));
    }

    function toast(msg) {
        var t = el('ap-toast');
        t.textContent = msg;
        t.classList.add('ap-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('ap-open');
        }, 3200);
    }

    function openNav() {
        el('ap-rail').classList.add('ap-open');
        el('ap-scrim').classList.add('ap-open');
    }

    function closeNav() {
        el('ap-rail').classList.remove('ap-open');
        el('ap-scrim').classList.remove('ap-open');
    }

    function openModal(title) {
        el('ap-mtitle').textContent = title;
        el('ap-modal').classList.add('ap-open');
        dirty = false;
        var f = el('ap-mbody').querySelector('input, select, textarea');
        if (f) {
            f.focus();
        }
    }

    function forceClose() {
        el('ap-modal').classList.remove('ap-open');
        el('ap-mbody').innerHTML = '';
        dirty = false;
    }

    function closeModal() {
        if (!el('ap-modal').classList.contains('ap-open')) {
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
        if (!askBox || !askBox.classList.contains('ap-open')) {
            return;
        }
        var x = askParts(), f = askDone, v = x.i.value.trim();
        askBox.classList.remove('ap-open');
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
        askBox.className = 'ap-ask';
        askBox.innerHTML = '<div class="ap-askbox" role="alertdialog" aria-modal="true"><h2></h2><p></p><input type="text" maxlength="300" autocomplete="off" aria-label="Reason"><em class="ap-err" role="alert"></em><div class="ap-askacts"><button type="button" class="ap-btn ap-btn-ghost"></button><button type="button" class="ap-btn"></button></div></div>';
        el('ap-modal').parentNode.appendChild(askBox);
        var x = askParts();
        x.b[0].addEventListener('click', function () { askClose(false); });
        x.b[1].addEventListener('click', askOk);
        x.i.addEventListener('input', function () { x.e.textContent = ''; });
        askBox.addEventListener('mousedown', function (e) { if (e.target === askBox) { askClose(false); } });
        window.addEventListener('keydown', function (e) {
            if (!askBox.classList.contains('ap-open')) {
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
        x.b[1].className = 'ap-btn' + (o.danger ? ' ap-askdel' : '');
        askBox.firstChild.classList.toggle('ap-askin', input);
        x.i.value = input ? o.value : '';
        x.i.placeholder = o.place || '';
        askBox.classList.add('ap-open');
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
            { key: 'fterm', vlu: val('ap-term') },
            { key: 'fcat', vlu: val('ap-cat') },
            { key: 'page', vlu: String(pageNo) }
        ];
    }

    function list() {
        api('List', [
            { key: 'term', vlu: val('ap-term') },
            { key: 'cat', vlu: val('ap-cat') },
            { key: 'page', vlu: String(pageNo) }
        ]);
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

    function edit(id) {
        api('Edit', [{ key: 'id', vlu: id }]);
    }

    function pic(select) {
        var img = el('ap-pp');
        if (img) {
            img.src = 'images/products/' + select.value + '.webp';
        }
    }

    function save() {
        api('Save', [
            { key: 'id', vlu: val('ap-f-id') },
            { key: 'title', vlu: val('ap-f-title') },
            { key: 'category', vlu: val('ap-f-cat') },
            { key: 'price', vlu: val('ap-f-price') },
            { key: 'unit', vlu: val('ap-f-unit') },
            { key: 'text', vlu: val('ap-f-text') },
            { key: 'tag', vlu: val('ap-f-tag') },
            { key: 'image', vlu: val('ap-f-image') },
            { key: 'available', vlu: checked('ap-f-avail') },
            { key: 'featured', vlu: checked('ap-f-feat') }
        ].concat(keep()));
        return false;
    }

    function toggle(id, title, available) {
        ask({ title: available ? 'Mark ' + title + ' as sold out?' : 'Put ' + title + ' back on sale?', text: available ? 'Customers will not be able to order it until you turn it back on.' : 'Customers can order it again straight away.', ok: available ? 'Mark sold out' : 'Put back on sale', no: 'Keep as it is', danger: available }, function () {
            api('Toggle', keep().concat([{ key: 'id', vlu: id }]));
        });
    }

    function remove(id, title) {
        ask({ title: 'Delete ' + title + '?', text: 'It disappears from the menu. Past orders keep their own copy of the name and price.', ok: 'Delete product', no: 'Keep it', danger: true }, function () {
            api('Delete', keep().concat([{ key: 'id', vlu: id }]));
        });
    }

    function saved(message) {
        forceClose();
        toast(message);
    }

    function reveal() {
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && !(askBox && askBox.classList.contains('ap-open'))) {
                closeNav();
                closeModal();
            }
        });
        el('ap-modal').addEventListener('mousedown', function (e) {
            if (e.target === el('ap-modal')) {
                closeModal();
            }
        });
        el('ap-mbody').addEventListener('input', markDirty);
        el('ap-mbody').addEventListener('change', markDirty);
        el('ap-main').addEventListener('input', debounce(function (e) {
            if (e.target && e.target.id === 'ap-term') {
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
        refresh: refresh,
        changed: changed,
        go: go,
        edit: edit,
        pic: pic,
        save: save,
        toggle: toggle,
        remove: remove,
        saved: saved
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminProductsJs.reveal);
} else {
    AdminProductsJs.reveal();
}
