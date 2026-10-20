var AdminLoginJs = (function () {

    var choosing = false;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? e.value : '';
    }

    function basePath() {
        var p = location.pathname;
        return p.substring(0, p.lastIndexOf('/') + 1);
    }

    function submit() {
        var list = [{ key: 'login', vlu: val('lg-login').trim() }, { key: 'pass', vlu: val('lg-pass') }];
        if (choosing) {
            list.push({ key: 'new1', vlu: val('lg-new1') });
            list.push({ key: 'new2', vlu: val('lg-new2') });
            $ApiRequest('AdminLogin/SetPassword', JSON.stringify(list));
        } else {
            $ApiRequest('AdminLogin/SignIn', JSON.stringify(list));
        }
        el('lg-go').disabled = true;
        return false;
    }

    function choose() {
        choosing = true;
        el('lg-choose').hidden = false;
        el('lg-go').textContent = 'Save and sign in';
        el('lg-go').disabled = false;
        el('lg-plabel').textContent = 'Current password';
        el('lg-new1').focus();
    }

    function failed() {
        el('lg-go').disabled = false;
    }

    function signedIn(token) {
        document.cookie = 'bk_tok=' + token + '; path=' + basePath() + '; max-age=28800; samesite=lax';
        location.href = 'Admin';
    }

    function reveal() {
        var first = el('lg-login');
        if (first) {
            first.focus();
        }
    }

    return {
        reveal: reveal,
        submit: submit,
        choose: choose,
        failed: failed,
        signedIn: signedIn
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', AdminLoginJs.reveal);
} else {
    AdminLoginJs.reveal();
}
