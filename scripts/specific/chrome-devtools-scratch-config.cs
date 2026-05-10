return 0;

namespace Scripts
{
    internal static class CdpRun
    {
        public const string Command = "evaluate_script";
        public const string Function = "() => JSON.stringify({url: location.href, sess: localStorage.getItem('wolfs_session'), email: localStorage.getItem('wolfs_email'), role: localStorage.getItem('wolfs_role'), sso: localStorage.getItem('wolfs_sso'), header: Array.from(document.querySelectorAll('.TopActions a, .TopActions button')).map(x => (x.textContent||'').trim()).filter(Boolean)})";
        public const int PageId = 1;
    }
}
