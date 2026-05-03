using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace SharedUI.Services;

public sealed class WolfsJsBootstrap(IJSRuntime Js)
{
    private const string EvalIdentifier = "eval";

    private bool Installed;

    private const string JsBody =
        "(function () {" + "\n" +
        "  if (window.WolfsInterop) return;" + "\n" +
        "  const w = {};" + "\n" +
        "  const WORKER = '" + Domain.Constants.WorkerConstants.Origin + "';" + "\n" +
        "  const maps = new Map();" + "\n" +
        "  w.mapInit = function (id, lat, lng, zoom, theme) {" + "\n" +
        "    if (!window.L) throw new Error('Leaflet not loaded');" + "\n" +
        "    if (maps.has(id)) { try { maps.get(id).remove(); } catch (_) {} maps.delete(id); }" + "\n" +
        "    const m = L.map(id, { zoomControl: false, maxZoom: 19 }).setView([lat, lng], zoom);" + "\n" +
        "    L.control.zoom({ position: 'bottomleft' }).addTo(m);" + "\n" +
        "    const tileUrl = theme === 'dark'" + "\n" +
        "      ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'" + "\n" +
        "      : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';" + "\n" +
        "    L.tileLayer(tileUrl, { maxZoom: 19, subdomains: 'abcd' }).addTo(m);" + "\n" +
        "    maps.set(id, m);" + "\n" +
        "    return true;" + "\n" +
        "  };" + "\n" +
        "  w.mapSetView = function (id, lat, lng, zoom) { const m = maps.get(id); if (m) m.setView([lat, lng], zoom); };" + "\n" +
        "  w.mapPanBy = function (id, dx, dy) { const m = maps.get(id); if (m) m.panBy([dx, dy], { animate: false }); };" + "\n" +
        "  w.mapDestroy = function (id) { const m = maps.get(id); if (m) { m.remove(); maps.delete(id); } };" + "\n" +
        "  w.mapAddPin = function (id, lat, lng, color, label) {" + "\n" +
        "    const m = maps.get(id); if (!m) return null;" + "\n" +
        """    const html = '<div style="width:30px;height:30px;border-radius:50%;background:' + (color || '#9a3a10') +""" + "\n" +
        """                 ';border:3px solid #ffffff;box-shadow:0 0 0 1px #cbd5e1;display:flex;align-items:center;justify-content:center;color:#fff;font-weight:800">' + (label || '') + '</div>';""" + "\n" +
        "    return L.marker([lat, lng], { icon: L.divIcon({ className: '', html, iconSize: [30, 30], iconAnchor: [15, 15] }) }).addTo(m);" + "\n" +
        "  };" + "\n" +
        "  w.mapDrawRoute = function (id, coords, color) {" + "\n" +
        "    const m = maps.get(id); if (!m || !coords || coords.length < 2) return;" + "\n" +
        "    const line = L.polyline(coords, { color: color || '#3b82f6', weight: 6, opacity: .9 }).addTo(m);" + "\n" +
        "    try { m.fitBounds(line.getBounds(), { padding: [40, 40] }); } catch (_) {}" + "\n" +
        "    return line;" + "\n" +
        "  };" + "\n" +
        "  function dbOpen() {" + "\n" +
        "    return new Promise((res, rej) => {" + "\n" +
        "      const r = indexedDB.open('wolfs', 1);" + "\n" +
        "      r.onupgradeneeded = () => {" + "\n" +
        "        const d = r.result;" + "\n" +
        "        ['users','workers','jobs','timesheets','applicants','listings','purchases','badges','roles','customers','audit'].forEach(s => {" + "\n" +
        "          if (!d.objectStoreNames.contains(s)) d.createObjectStore(s, { keyPath: 'id' });" + "\n" +
        "        });" + "\n" +
        "      };" + "\n" +
        "      r.onsuccess = () => res(r.result);" + "\n" +
        "      r.onerror = () => rej(r.error);" + "\n" +
        "    });" + "\n" +
        "  }" + "\n" +
        "  async function dbAll(store) {" + "\n" +
        "    const db = await dbOpen();" + "\n" +
        "    return await new Promise((res, rej) => {" + "\n" +
        "      const tx = db.transaction(store, 'readonly');" + "\n" +
        "      const req = tx.objectStore(store).getAll();" + "\n" +
        "      req.onsuccess = () => res(req.result || []);" + "\n" +
        "      req.onerror = () => rej(req.error);" + "\n" +
        "    });" + "\n" +
        "  }" + "\n" +
        "  async function dbPut(store, val) {" + "\n" +
        "    const db = await dbOpen();" + "\n" +
        "    return await new Promise((res, rej) => {" + "\n" +
        "      const tx = db.transaction(store, 'readwrite');" + "\n" +
        "      tx.objectStore(store).put(val);" + "\n" +
        "      tx.oncomplete = () => res(true);" + "\n" +
        "      tx.onerror = () => rej(tx.error);" + "\n" +
        "    });" + "\n" +
        "  }" + "\n" +
        "  async function dbGet(store, id) {" + "\n" +
        "    const db = await dbOpen();" + "\n" +
        "    return await new Promise((res, rej) => {" + "\n" +
        "      const tx = db.transaction(store, 'readonly');" + "\n" +
        "      const req = tx.objectStore(store).get(id);" + "\n" +
        "      req.onsuccess = () => res(req.result || null);" + "\n" +
        "      req.onerror = () => rej(req.error);" + "\n" +
        "    });" + "\n" +
        "  }" + "\n" +
        "  w.dbAll = dbAll; w.dbPut = dbPut; w.dbGet = dbGet;" + "\n" +
        "  w.dbAllJson = async function (store) { return JSON.stringify(await dbAll(store)); };" + "\n" +
        "  w.setMapHeight = function (id, h) { var el = document.getElementById(id); if (el) el.style.height = h + 'px'; };" + "\n" +
        "  w.chatReply = async function (system, history, maxTokens) {" + "\n" +
        "    var sess = localStorage.getItem('wolfs_session') || '';" + "\n" +
        "    var role = localStorage.getItem('wolfs_role') || 'client';" + "\n" +
        "    var resp = await fetch(WORKER + '/ai', {" + "\n" +
        "      method: 'POST'," + "\n" +
        "      headers: { 'Content-Type': 'application/json', 'X-Wolfs-Session': sess, 'X-Wolfs-Role': role }," + "\n" +
        "      body: JSON.stringify({ messages: history, system: system, max_tokens: maxTokens || 256 })," + "\n" +
        "    });" + "\n" +
        "    var data = await resp.json().catch(function () { return null; });" + "\n" +
        "    return (data && data.text) || (data && data.error) || 'Sorry, no reply.';" + "\n" +
        "  };" + "\n" +
        "  w.startCall = async function (role, subject) {" + "\n" +
        "    if (!navigator.mediaDevices) return 'mic unavailable';" + "\n" +
        "    try {" + "\n" +
        "      var pc = new RTCPeerConnection({ iceServers: [{ urls: 'stun:stun.cloudflare.com:3478' }] });" + "\n" +
        "      var stream = await navigator.mediaDevices.getUserMedia({ audio: true });" + "\n" +
        "      stream.getTracks().forEach(function (t) { pc.addTrack(t, stream); });" + "\n" +
        "      pc.addTransceiver('audio', { direction: 'recvonly' });" + "\n" +
        "      var offer = await pc.createOffer();" + "\n" +
        "      await pc.setLocalDescription(offer);" + "\n" +
        "      var sess = localStorage.getItem('wolfs_session') || '';" + "\n" +
        "      var resp = await fetch(WORKER + '/voice', {" + "\n" +
        "        method: 'POST'," + "\n" +
        "        headers: { 'Content-Type': 'application/json', 'X-Wolfs-Session': sess, 'X-Wolfs-Role': role || 'client' }," + "\n" +
        "        body: JSON.stringify({ sdp: offer.sdp, type: 'offer', subject: subject || '' })," + "\n" +
        "      });" + "\n" +
        "      if (!resp.ok) {" + "\n" +
        "        try { pc.close(); } catch (e) {}" + "\n" +
        "        stream.getTracks().forEach(function (t) { t.stop(); });" + "\n" +
        "        var j = await resp.json().catch(function () { return null; });" + "\n" +
        "        return 'realtime unavailable: ' + ((j && j.error) || resp.status);" + "\n" +
        "      }" + "\n" +
        "      var answer = await resp.json();" + "\n" +
        "      await pc.setRemoteDescription({ sdp: answer.sdp, type: 'answer' });" + "\n" +
        "      pc.ontrack = function (ev) { var a = new Audio(); a.srcObject = ev.streams[0]; a.play().catch(function () {}); };" + "\n" +
        "      window.__wolfsCallPc = pc; window.__wolfsCallStream = stream;" + "\n" +
        "      return 'connected';" + "\n" +
        "    } catch (e) { return 'error: ' + e.message; }" + "\n" +
        "  };" + "\n" +
        "  w.__rec = null;" + "\n" +
        "  w.__recPending = null;" + "\n" +
        "  w.recognize = function () {" + "\n" +
        "    try { speechSynthesis.cancel(); } catch (e) {}" + "\n" +
        "    return new Promise(function (resolve) {" + "\n" +
        "      var SR = window.SpeechRecognition || window.webkitSpeechRecognition;" + "\n" +
        "      if (!SR) { resolve({ error: 'speech-recognition-unavailable' }); return; }" + "\n" +
        "      if (w.__rec) { try { w.__rec.stop(); } catch (e) {} }" + "\n" +
        "      var rec = new SR();" + "\n" +
        "      w.__rec = rec;" + "\n" +
        "      rec.lang = 'en-US';" + "\n" +
        "      rec.continuous = true;" + "\n" +
        "      rec.interimResults = false;" + "\n" +
        "      var collected = [];" + "\n" +
        "      var done = false;" + "\n" +
        "      function finish(payload) { if (done) return; done = true; if (w.__rec === rec) { w.__rec = null; } resolve(payload); }" + "\n" +
        "      rec.onspeechstart = function () { try { document.body.setAttribute('data-wolfs-speaking', '1'); } catch (e) {} };" + "\n" +
        "      rec.onspeechend = function () { try { document.body.removeAttribute('data-wolfs-speaking'); } catch (e) {} };" + "\n" +
        "      rec.onresult = function (ev) { for (var i = ev.resultIndex; i < ev.results.length; i++) { collected.push(ev.results[i][0].transcript); } };" + "\n" +
        "      rec.onerror = function (ev) { finish({ error: ev.error || 'recognition-error', text: collected.join(' ') }); };" + "\n" +
        "      rec.onend = function () { finish({ text: collected.join(' ') }); };" + "\n" +
        "      try { rec.start(); } catch (e) { finish({ error: e.message }); }" + "\n" +
        "    });" + "\n" +
        "  };" + "\n" +
        "  w.recognizeStop = function () { try { if (w.__rec) { w.__rec.stop(); } } catch (e) {} };" + "\n" +
        "  w.cancelSpeak = function () { try { speechSynthesis.cancel(); } catch (e) {} };" + "\n" +
        "  w.speak = function (text, voice) {" + "\n" +
        "    try {" + "\n" +
        "      try { speechSynthesis.cancel(); } catch (e) {}" + "\n" +
        "      var clean = (text || '')" + "\n" +
        "        .replace(/\\$\\d+/g, '')" + "\n" +
        "        .replace(/\\*+/g, '')" + "\n" +
        "        .replace(/[\\u{1F300}-\\u{1FAFF}\\u{2600}-\\u{27BF}\\u{1F000}-\\u{1F2FF}\\u{1F900}-\\u{1F9FF}]/gu, '')" + "\n" +
        "        .replace(/[#`~_>|]/g, '')" + "\n" +
        "        .replace(/\\s+/g, ' ').trim();" + "\n" +
        "      if (!clean) { return; }" + "\n" +
        "      var u = new SpeechSynthesisUtterance(clean);" + "\n" +
        "      var voices = speechSynthesis.getVoices().filter(function(v){ return /^en[-_]/i.test(v.lang); });" + "\n" +
        "      var preferred = voices.find(function(v){ return /natural|online|aria|jenny|guy|david|zira/i.test(v.name); }) || voices.find(function(v){ return /^en-US/i.test(v.lang); }) || voices[0];" + "\n" +
        "      if (preferred) { u.voice = preferred; }" + "\n" +
        "      u.lang = (preferred && preferred.lang) || 'en-US';" + "\n" +
        "      u.pitch = 1.0;" + "\n" +
        "      u.rate = 1.0;" + "\n" +
        "      speechSynthesis.speak(u);" + "\n" +
        "    } catch (e) {}" + "\n" +
        "  };" + "\n" +
        "  w.scrollChatBottom = function () {" + "\n" +
        "    try {" + "\n" +
        "      var s = document.querySelector('.ChatStream');" + "\n" +
        "      if (s) { s.scrollTop = s.scrollHeight; }" + "\n" +
        "      window.scrollTo(0, document.body.scrollHeight);" + "\n" +
        "    } catch (e) {}" + "\n" +
        "  };" + "\n" +
        "  w.endCall = function () {" + "\n" +
        "    try { if (window.__wolfsCallPc) window.__wolfsCallPc.close(); } catch (e) {}" + "\n" +
        "    try { if (window.__wolfsCallStream) window.__wolfsCallStream.getTracks().forEach(function (t) { t.stop(); }); } catch (e) {}" + "\n" +
        "    window.__wolfsCallPc = null; window.__wolfsCallStream = null;" + "\n" +
        "  };" + "\n" +
        "  w.themeCycle = function () {" + "\n" +
        "    var cur = localStorage.getItem('wolfs_theme') || 'auto';" + "\n" +
        "    var next = cur === 'auto' ? 'dark' : (cur === 'dark' ? 'light' : 'auto');" + "\n" +
        "    try {" + "\n" +
        "      localStorage.setItem('wolfs_theme', next);" + "\n" +
        "      document.documentElement.setAttribute('data-theme', next === 'auto' ? '' : next);" + "\n" +
        "    } catch (e) {}" + "\n" +
        "    return next;" + "\n" +
        "  };" + "\n" +
        "  w.ssoLogin = function (provider) {" + "\n" +
        "    var p = (provider || 'demo').toLowerCase();" + "\n" +
        "    var params = new URLSearchParams(location.search);" + "\n" +
        "    var realEmail = params.get('email');" + "\n" +
        "    var realSession = params.get('session');" + "\n" +
        "    try {" + "\n" +
        "      w.authClear();" + "\n" +
        "      localStorage.setItem('wolfs_session', realSession || ('sso-' + p + '-' + Date.now()));" + "\n" +
        "      localStorage.setItem('wolfs_role', 'user');" + "\n" +
        "      localStorage.setItem('wolfs_email', realEmail || ('demo@' + p + '.example'));" + "\n" +
        "      localStorage.setItem('wolfs_sso', p);" + "\n" +
        "    } catch (e) {}" + "\n" +
        """    var base = location.pathname.replace(/Login\/?$/, '');""" + "\n" +
        "    location.href = base + 'Marketplace/';" + "\n" +
        "  };" + "\n" +
        "  // Pre-hydration SSO autoload: if the URL has ?sso=<provider>, run the" + "\n" +
        "  // demo session set + redirect immediately. This way the static /Login/" + "\n" +
        "  // page works even before Blazor WASM hydrates the @onclick handler." + "\n" +
        "  (function () {" + "\n" +
        "    try {" + "\n" +
        "      var m = location.search.match(/[?&]sso=([a-z]+)/i);" + "\n" +
        "      if (m) { w.ssoLogin(m[1]); }" + "\n" +
        "      var sm = location.search.match(/[?&]signout=1/i);" + "\n" +
        "      if (sm) { w.authClear(); var soBase = location.pathname.replace(/Settings\\/?$/, ''); location.replace(soBase); }" + "\n" +
        "    } catch (e) {}" + "\n" +
        "  })();" + "\n" +
        "  w.paintHeader = function () {" + "\n" +
        "    try {" + "\n" +
        "      var role = localStorage.getItem('wolfs_role') || '';" + "\n" +
        "      var email = localStorage.getItem('wolfs_email') || '';" + "\n" +
        "      if (!role) { return; }" + "\n" +
        "      var roleLabel = ({client:'buyer',customer:'buyer',buyer:'buyer',shipper:'seller',seller:'seller',admin:'admin',administrator:'admin',driver:'driver',carrier:'driver'})[(role || '').toLowerCase()] || role || 'buyer';" + "\n" +
        "      var actions = document.querySelector('.TopActions');" + "\n" +
        "      if (!actions) { return; }" + "\n" +
        "      var anchors = actions.querySelectorAll('a');" + "\n" +
        "      for (var i = 0; i < anchors.length; i++) {" + "\n" +
        "        var a = anchors[i];" + "\n" +
        "        var text = (a.textContent || '').trim();" + "\n" +
        "        if (text === 'Sign In' || text === 'Log off' || text.indexOf('Log off') === 0) {" + "\n" +
        "          a.outerHTML = '<a href=\"?signout=1\" class=\"WolfsLogoff\">Log Off (' + (email || 'signed-in user') + ') - ' + roleLabel + '</a>';" + "\n" +
        "          break;" + "\n" +
        "        }" + "\n" +
        "      }" + "\n" +
        "    } catch (e) {}" + "\n" +
        "  };" + "\n" +
        "  (function () {" + "\n" +
        "    try {" + "\n" +
        "      var wm = location.search.match(/[?&]wsso=([a-z]+)/i);" + "\n" +
        "      if (wm) {" + "\n" +
        "        var qp = new URLSearchParams(location.search);" + "\n" +
        "        try {" + "\n" +
        "          w.authClear();" + "\n" +
        "          localStorage.setItem('wolfs_role', 'user');" + "\n" +
        "          if (qp.get('email')) localStorage.setItem('wolfs_email', qp.get('email'));" + "\n" +
        "          if (qp.get('session')) localStorage.setItem('wolfs_session', qp.get('session'));" + "\n" +
        "          localStorage.setItem('wolfs_sso', wm[1].toLowerCase());" + "\n" +
        "        } catch (e) {}" + "\n" +
        "        history.replaceState(null, '', location.pathname);" + "\n" +
        "      }" + "\n" +
        "      if (document.readyState === 'loading') {" + "\n" +
        "        document.addEventListener('DOMContentLoaded', w.paintHeader);" + "\n" +
        "      } else {" + "\n" +
        "        w.paintHeader();" + "\n" +
        "      }" + "\n" +
        "    } catch (e) {}" + "\n" +
        "  })();" + "\n" +
        "  w.workerPost = async function (path, body, headers) {" + "\n" +
        "    var sess = localStorage.getItem('wolfs_session') || '';" + "\n" +
        "    var email = localStorage.getItem('wolfs_email') || '';" + "\n" +
        "    var role = localStorage.getItem('wolfs_role') || '';" + "\n" +
        "    var defaults = { 'Content-Type': 'application/json' };" + "\n" +
        "    if (sess) defaults['X-Wolfs-Session'] = sess;" + "\n" +
        "    if (email) defaults['X-Wolfs-Email'] = email;" + "\n" +
        "    if (role) defaults['X-Wolfs-Role'] = role;" + "\n" +
        "    const r = await fetch(WORKER + path, {" + "\n" +
        "      method: 'POST'," + "\n" +
        "      headers: Object.assign(defaults, headers || {})," + "\n" +
        "      body: typeof body === 'string' ? body : JSON.stringify(body || {})," + "\n" +
        "    });" + "\n" +
        "    const txt = await r.text();" + "\n" +
        "    return { ok: r.ok, status: r.status, body: txt };" + "\n" +
        "  };" + "\n" +
        "  w.workerGet = async function (path) {" + "\n" +
        "    var sess = localStorage.getItem('wolfs_session') || '';" + "\n" +
        "    var email = localStorage.getItem('wolfs_email') || '';" + "\n" +
        "    var role = localStorage.getItem('wolfs_role') || '';" + "\n" +
        "    var hdrs = {};" + "\n" +
        "    if (sess) hdrs['X-Wolfs-Session'] = sess;" + "\n" +
        "    if (email) hdrs['X-Wolfs-Email'] = email;" + "\n" +
        "    if (role) hdrs['X-Wolfs-Role'] = role;" + "\n" +
        "    const r = await fetch(WORKER + path, { headers: hdrs });" + "\n" +
        "    return { ok: r.ok, status: r.status, body: await r.text() };" + "\n" +
        "  };" + "\n" +
        "  w.themeRead = function () { return localStorage.getItem('wolfs_theme') || 'auto'; };" + "\n" +
        "  w.themeWrite = function (v) { try { localStorage.setItem('wolfs_theme', v); document.documentElement.setAttribute('data-theme', v === 'auto' ? '' : v); } catch (_) {} };" + "\n" +
        "  w.themeResolved = function () {" + "\n" +
        "    const t = w.themeRead();" + "\n" +
        "    if (t === 'auto') return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';" + "\n" +
        "    return t;" + "\n" +
        "  };" + "\n" +
        "  w.authGet = function () {" + "\n" +
        "    return {" + "\n" +
        "      role: (localStorage.getItem('wolfs_role') || '').toLowerCase() || null," + "\n" +
        "      email: localStorage.getItem('wolfs_email') || null," + "\n" +
        "      sess:  localStorage.getItem('wolfs_session') || null," + "\n" +
        "    };" + "\n" +
        "  };" + "\n" +
        "  w.authSet = function (role, email, sess) {" + "\n" +
        "    if (role) localStorage.setItem('wolfs_role', role); else localStorage.removeItem('wolfs_role');" + "\n" +
        "    if (email) localStorage.setItem('wolfs_email', email); else localStorage.removeItem('wolfs_email');" + "\n" +
        "    if (sess) localStorage.setItem('wolfs_session', sess); else localStorage.removeItem('wolfs_session');" + "\n" +
        "  };" + "\n" +
        "  w.authClear = function () { ['wolfs_role','wolfs_email','wolfs_session','wolfs_sso'].forEach(k => localStorage.removeItem(k)); };" + "\n" +
        "  window.WolfsInterop = w;" + "\n" +
        "})();" + "\n";

    public async ValueTask EnsureInstalledAsync()
    {
        if (Installed) { return; }
        Installed = true;
        await Js.InvokeVoidAsync(EvalIdentifier, JsBody);
    }
}
