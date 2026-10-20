var HomeJs = (function () {

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
        $ApiRequest('Home/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('hm-toast');
        t.textContent = msg;
        t.classList.add('hm-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('hm-open');
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
        var c = el('hm-count'), n = cartTotal();
        if (c) {
            c.textContent = n;
            c.classList.toggle('hm-zero', n === 0);
        }
    }

    function openNav() {
        el('hm-nav').classList.add('hm-open');
        el('hm-scrim').classList.add('hm-open');
    }

    function closeNav() {
        el('hm-nav').classList.remove('hm-open');
        el('hm-scrim').classList.remove('hm-open');
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
        closeNav: closeNav
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', HomeJs.reveal);
} else {
    HomeJs.reveal();
}
