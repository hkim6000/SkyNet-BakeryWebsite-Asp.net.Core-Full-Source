var MenuJs = (function () {

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
        $ApiRequest('Menu/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('mn-toast');
        t.textContent = msg;
        t.classList.add('mn-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('mn-open');
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
        var c = el('mn-count'), n = cartTotal();
        if (c) {
            c.textContent = n;
            c.classList.toggle('mn-zero', n === 0);
        }
    }

    function openNav() {
        el('mn-nav').classList.add('mn-open');
        el('mn-scrim').classList.add('mn-open');
    }

    function closeNav() {
        el('mn-nav').classList.remove('mn-open');
        el('mn-scrim').classList.remove('mn-open');
    }

    var timer = null;

        function current() {
        var c = el('mn-chips');
        return c ? c.getAttribute('data-cat') || '' : '';
    }

    function filter(cat) {
        call('Filter', [{ key: 'cat', vlu: cat }, { key: 'term', vlu: val('mn-q') }]);
    }

    function search(value) {
        clearTimeout(timer);
        timer = setTimeout(function () {
            filter(current());
        }, 250);
    }

    function filtered(cat, term) {
        el('mn-chips').setAttribute('data-cat', cat);
        var p = new URLSearchParams();
        if (cat) {
            p.set('c', cat);
        }
        if (term) {
            p.set('s', term);
        }
        var qs = p.toString();
        history.replaceState(null, '', 'Menu' + (qs ? '?' + qs : ''));
    }

    function add(btn) {
        var key = btn.getAttribute('data-key'), title = btn.getAttribute('data-title');
        var cart = readCart();
        var n = (Number(cart[key]) || 0) + 1;
        if (n > 20) {
            toast('You can order up to 20 of one item. Call us for larger orders.');
            return;
        }
        cart[key] = n;
        writeCart(cart);
        toast('Added ' + title + ' (' + n + ' in your basket)');
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
        filter: filter,
        search: search,
        filtered: filtered,
        add: add
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', MenuJs.reveal);
} else {
    MenuJs.reveal();
}
