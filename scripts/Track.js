var TrackJs = (function () {

    var ttimer = null;
    var memory = null;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function call(method, list) {
        $ApiRequest('Track/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('tr-toast');
        t.textContent = msg;
        t.classList.add('tr-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('tr-open');
        }, 3000);
    }

    function readCart() {
        if (memory) {
            return memory;
        }
        try {
            var raw = JSON.parse(localStorage.getItem('hb_cart') || '{}');
            return raw && typeof raw === 'object' ? raw : {};
        } catch (e) {
            return {};
        }
    }

    function writeCart(cart) {
        memory = cart;
        try {
            localStorage.setItem('hb_cart', JSON.stringify(cart));
        } catch (e) {
            memory = cart;
        }
        showCount();
    }

    function cartTotal() {
        var cart = readCart(), n = 0;
        Object.keys(cart).forEach(function (k) {
            n += Number(cart[k]) || 0;
        });
        return n;
    }

    function showCount() {
        var c = el('tr-count'), n = cartTotal();
        if (c) {
            c.textContent = n;
            c.classList.toggle('tr-zero', n === 0);
        }
    }

    function openNav() {
        el('tr-nav').classList.add('tr-open');
        el('tr-scrim').classList.add('tr-open');
    }

    function closeNav() {
        el('tr-nav').classList.remove('tr-open');
        el('tr-scrim').classList.remove('tr-open');
    }

    function find() {
        call('Find', [{ key: 'num', vlu: val('tr-num') }, { key: 'mail', vlu: val('tr-email') }]);
        return false;
    }

    function reveal() {
        showCount();
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
            }
        });
    }

    return {
        reveal: reveal,
        toast: toast,
        openNav: openNav,
        closeNav: closeNav,
        find: find
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', TrackJs.reveal);
} else {
    TrackJs.reveal();
}
