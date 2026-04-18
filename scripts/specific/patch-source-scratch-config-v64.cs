return 0;

namespace Scripts
{
    internal static class PatchSourceScratchConfigV64
    {
        public const string TargetFile = @"C:\repo\public\wolfstruckingco.com\main\src\SharedUI\Pages\LoginPage.razor";

        public const string Find_01 = "            <a class=\"SsoBtn\" href=\"https://wolfstruckingco.nbth.workers.dev/oauth/okta/start\">🔑 Okta</a>\n        </div>\n    </div>\n</div>\n<script>(function(){try{var p=new URLSearchParams(location.search);var s=p.get('sso');if(!s)return;var e=p.get('email')||'';var k=p.get('session')||('sso-'+s+'-'+Date.now());try{localStorage.setItem('wolfs_session',k);localStorage.setItem('wolfs_role','user');localStorage.setItem('wolfs_email',e);}catch(_){}var i=location.pathname.lastIndexOf('Login');var b=i>=0?location.pathname.substring(0,i):location.pathname;location.replace(b+'Marketplace/');}catch(_){}})();</script>";
        public const string Replace_01 = "            <a class=\"SsoBtn\" href=\"https://wolfstruckingco.nbth.workers.dev/oauth/okta/start\">🔑 Okta</a>\n        </div>\n    </div>\n</div>";

        public const string Find_02 = "___UNUSED_SLOT___";
        public const string Replace_02 = "";
    }
}
