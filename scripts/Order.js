var OrderJs = (function () {

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
        $ApiRequest('Order/' + method, JSON.stringify(list || []));
    }

    function toast(msg) {
        var t = el('od-toast');
        t.textContent = msg;
        t.classList.add('od-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('od-open');
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
        var c = el('od-count'), n = cartTotal();
        if (c) {
            c.textContent = n;
            c.classList.toggle('od-zero', n === 0);
        }
    }

    function openNav() {
        el('od-nav').classList.add('od-open');
        el('od-scrim').classList.add('od-open');
    }

    function closeNav() {
        el('od-nav').classList.remove('od-open');
        el('od-scrim').classList.remove('od-open');
    }

    var timer = null;

    function cartString() {
        var cart = readCart();
        return Object.keys(cart).filter(function (k) {
            return Number(cart[k]) > 0;
        }).map(function (k) {
            return k + ':' + Number(cart[k]);
        }).join(';');
    }

    function modeValue() {
        return el('od-m-delivery').checked ? 'delivery' : 'pickup';
    }

    function base() {
        return [
            { key: 'cart', vlu: cartString() },
            { key: 'mode', vlu: modeValue() },
            { key: 'date', vlu: val('od-date') },
            { key: 'slot', vlu: val('od-slot') }
        ];
    }

    function requote(fresh) {
        clearTimeout(timer);
        timer = setTimeout(function () {
            var list = base();
            if (fresh === true) {
                list.push({ key: 'fresh', vlu: '1' });
            }
            call('Quote', list);
        }, 60);
    }

    function mode() {
        el('od-deliv').hidden = modeValue() !== 'delivery';
        requote();
    }

    function step(key, delta) {
        var cart = readCart();
        var n = (Number(cart[key]) || 0) + delta;
        if (n > 20) {
            toast('You can order up to 20 of one item. Call us for larger orders.');
            return;
        }
        if (n < 1) {
            delete cart[key];
        } else {
            cart[key] = n;
        }
        writeCart(cart);
        requote();
    }

    function drop(key) {
        var cart = readCart();
        delete cart[key];
        writeCart(cart);
        requote();
    }

    function cartSet(text) {
        var cart = {};
        (text || '').split(';').forEach(function (part) {
            var bits = part.split(':');
            if (bits.length === 2 && Number(bits[1]) > 0) {
                cart[bits[0]] = Number(bits[1]);
            }
        });
        if (JSON.stringify(cart) !== JSON.stringify(readCart())) {
            writeCart(cart);
        }
    }

    function place() {
        var list = base();
        ['name', 'email', 'phone', 'address', 'zip', 'note'].forEach(function (f) {
            list.push({ key: f, vlu: val('od-' + f) });
        });
        el('od-place').disabled = true;
        call('Place', list);
        return false;
    }

    function failed() {
        el('od-place').disabled = false;
    }

    function placed() {
        writeCart({});
        el('od-layout').classList.add('od-gone');
        el('od-done').scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function reveal() {
        showCount();
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                closeNav();
            }
        });
        requote(true);
    }

    return {
        reveal: reveal,
        toast: toast,
        openNav: openNav,
        closeNav: closeNav,
        requote: requote,
        mode: mode,
        step: step,
        drop: drop,
        cartSet: cartSet,
        place: place,
        failed: failed,
        placed: placed
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', OrderJs.reveal);
} else {
    OrderJs.reveal();
}
