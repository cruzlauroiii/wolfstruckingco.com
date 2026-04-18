// Turn-by-turn voice navigation for the driver page.
//   const nav = WolfsNav.start({
//     map: leafletMap,
//     origin: [lat,lng],
//     destination: [lat,lng],
//     stops: [[lat,lng], …],
//     onTurn: ({ instruction, distanceM, stepIndex, totalSteps }) => {…},
//     onArrive: () => {…},
//     onOffRoute: (newRoute) => {…},
//     simulatedPath: optional [[lat,lng],…] for demo without real GPS
//   });
//   nav.stop();
//
// Routing: Valhalla FOSSGIS demo (`valhalla1.openstreetmap.de`). Returns voice-ready
// `verbal_pre_transition_instruction` per maneuver, no API key, fair-use rate limit.
// TTS: pre-generates audio for every maneuver via the existing /sidecar/tts endpoint and
// caches as Blob URLs so each turn announcement plays with no latency.
// Off-route detection: turf.nearestPointOnLine; reroute after 4 fixes >50m off the route.

(function (global) {
  'use strict';

  const VALHALLA = 'https://valhalla1.openstreetmap.de/route';
  const SIDECAR = (typeof location !== 'undefined' && location.protocol === 'https:') ? '/sidecar' : 'http://localhost:9334';

  // Loose Turf-style nearest-point-on-line — bundled to avoid the 200KB Turf import.
  // Returns { distanceM, indexAlong, snappedLatLng }.
  function nearestPointOnLine(point, lineCoords) {
    if (!lineCoords || lineCoords.length < 2) return { distanceM: Infinity, indexAlong: 0, snappedLatLng: point };
    let best = { d: Infinity, idx: 0, snap: lineCoords[0] };
    for (let i = 0; i < lineCoords.length - 1; i++) {
      const a = lineCoords[i];
      const b = lineCoords[i + 1];
      const snap = projectOnSegment(point, a, b);
      const d = haversineM(point, snap);
      if (d < best.d) best = { d, idx: i, snap };
    }
    return { distanceM: best.d, indexAlong: best.idx, snappedLatLng: best.snap };
  }

  function projectOnSegment(p, a, b) {
    // Project on the local-flat plane around `a` — accurate enough for <1km segments.
    const latRad = a[0] * Math.PI / 180;
    const k = Math.cos(latRad);
    const ax = a[1] * k, ay = a[0];
    const bx = b[1] * k, by = b[0];
    const px = p[1] * k, py = p[0];
    const dx = bx - ax, dy = by - ay;
    const len2 = dx * dx + dy * dy;
    if (!len2) return [a[0], a[1]];
    let t = ((px - ax) * dx + (py - ay) * dy) / len2;
    t = Math.max(0, Math.min(1, t));
    const sy = ay + t * dy;
    const sx = ax + t * dx;
    return [sy, sx / k];
  }

  function haversineM(a, b) {
    const R = 6371000;
    const toR = (d) => d * Math.PI / 180;
    const dLat = toR(b[0] - a[0]);
    const dLon = toR(b[1] - a[1]);
    const lat1 = toR(a[0]);
    const lat2 = toR(b[0]);
    const x = Math.sin(dLat / 2) ** 2 + Math.sin(dLon / 2) ** 2 * Math.cos(lat1) * Math.cos(lat2);
    return 2 * R * Math.asin(Math.sqrt(x));
  }

  // Decode a Google polyline (Valhalla returns precision 6).
  function decodePolyline(str, precision) {
    const factor = Math.pow(10, precision || 6);
    const result = [];
    let index = 0, lat = 0, lng = 0;
    while (index < str.length) {
      let b, shift = 0, r = 0;
      do { b = str.charCodeAt(index++) - 63; r |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
      lat += (r & 1) ? ~(r >> 1) : (r >> 1);
      shift = 0; r = 0;
      do { b = str.charCodeAt(index++) - 63; r |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
      lng += (r & 1) ? ~(r >> 1) : (r >> 1);
      result.push([lat / factor, lng / factor]);
    }
    return result;
  }

  async function fetchRouteOsrm(locations) {
    // Fallback when Valhalla is rate-limited / down. OSRM gives us coords for the polyline;
    // we synthesize "Continue on <street>" maneuvers so the banner has something to display.
    const coordsParam = locations.map(([lat, lng]) => lng + ',' + lat).join(';');
    const r = await fetch('https://router.project-osrm.org/route/v1/driving/' + coordsParam + '?overview=full&geometries=geojson&steps=true');
    if (!r.ok) throw new Error('OSRM ' + r.status);
    const data = await r.json();
    if (!data.routes || !data.routes.length) throw new Error('OSRM no route');
    const allCoords = data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);
    const allManeuvers = [];
    for (const leg of (data.routes[0].legs || [])) {
      for (const step of (leg.steps || [])) {
        const c = step.geometry && step.geometry.coordinates && step.geometry.coordinates[0];
        const name = step.name || step.maneuver?.modifier || 'route';
        const inst = (step.maneuver?.type || 'continue') + ' on ' + name;
        if (c) allManeuvers.push({ location: [c[1], c[0]], shapeIndex: 0, length: step.distance || 0, time: step.duration || 0, instruction: inst, verbalPre: inst, verbalPost: '', type: step.maneuver?.type, streetNames: name ? [name] : [] });
      }
    }
    return { coords: allCoords, maneuvers: allManeuvers, summary: data.routes[0] };
  }
  async function fetchRoute(locations) {
    const body = {
      locations: locations.map(([lat, lng]) => ({ lat, lon: lng, type: 'break' })),
      costing: 'truck',
      directions_options: { units: 'miles' },
      narrative: true,
    };
    let data = null;
    try {
      const r = await fetch(VALHALLA, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!r.ok) throw new Error('Valhalla ' + r.status);
      data = await r.json();
      if (!data.trip || !data.trip.legs || !data.trip.legs.length) throw new Error('no trip');
    } catch (ex) {
      // Valhalla failed — fall back to OSRM so the polyline still draws.
      try { return await fetchRouteOsrm(locations); }
      catch (osrmEx) { throw new Error('Valhalla + OSRM both failed: ' + ex.message + ' / ' + osrmEx.message); }
    }
    const allCoords = [];
    const allManeuvers = [];
    for (const leg of data.trip.legs) {
      const coords = decodePolyline(leg.shape, 6);
      const baseIdx = allCoords.length;
      for (const c of coords) allCoords.push(c);
      for (const m of (leg.maneuvers || [])) {
        const beginIdx = baseIdx + (m.begin_shape_index || 0);
        allManeuvers.push({
          location: allCoords[beginIdx] || allCoords[0],
          shapeIndex: beginIdx,
          length: m.length || 0,
          time: m.time || 0,
          instruction: m.instruction || '',
          verbalPre: m.verbal_pre_transition_instruction || m.verbal_alert_instruction || m.instruction || '',
          verbalPost: m.verbal_post_transition_instruction || '',
          type: m.type,
          streetNames: m.street_names || [],
        });
      }
    }
    return {
      coords: allCoords,
      maneuvers: allManeuvers,
      summary: data.trip.summary || {},
    };
  }

  async function ttsBlob(text) {
    try {
      const r = await fetch(SIDECAR + '/tts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text }),
      });
      if (!r.ok) return null;
      const blob = await r.blob();
      return URL.createObjectURL(blob);
    } catch (_) { return null; }
  }

  async function preGenerateVoice(maneuvers) {
    // Prefetch in parallel — sidecar handles its own queueing.
    const tasks = maneuvers.map(async (m) => {
      if (!m.verbalPre) return null;
      m.audioUrl = await ttsBlob(m.verbalPre);
      return m.audioUrl;
    });
    await Promise.allSettled(tasks);
  }

  function start(opts) {
    const map = opts.map;
    const origin = opts.origin;
    const destination = opts.destination;
    const stops = opts.stops || [];
    const onTurn = opts.onTurn || (() => {});
    const onArrive = opts.onArrive || (() => {});
    const onOffRoute = opts.onOffRoute || (() => {});
    const onProgress = opts.onProgress || (() => {});
    const triggerWithinM = opts.triggerWithinM || 200;
    const offRouteThresholdM = opts.offRouteThresholdM || 50;
    const offRouteFixesNeeded = opts.offRouteFixesNeeded || 4;
    const arriveWithinM = opts.arriveWithinM || 30;
    const simulatedPath = opts.simulatedPath || null;
    const simulatedTickMs = opts.simulatedTickMs || 700;

    if (!map || typeof L === 'undefined') throw new Error('Leaflet map required');
    if (!origin || !destination) throw new Error('origin + destination required');

    let route = null;
    let routeLine = null;
    let truckMarker = null;
    let watchId = null;
    let stopped = false;
    let announcedSteps = new Set();
    let consecutiveOff = 0;
    let simIdx = 0;
    let simTimer = null;

    async function loadRoute(originPt, destPt) {
      const locs = [originPt, ...stops, destPt];
      const r = await fetchRoute(locs);
      route = r;
      if (routeLine) { try { map.removeLayer(routeLine); } catch (_) {} }
      routeLine = L.polyline(r.coords, { color: '#3b82f6', weight: 5, opacity: .85 }).addTo(map);
      try { map.fitBounds(routeLine.getBounds(), { padding: [40, 40] }); } catch (_) {}
      // Drop maneuver pins (small blue dots) for visual reference.
      for (const mv of r.maneuvers) {
        L.circleMarker(mv.location, { radius: 4, color: '#3b82f6', fillColor: '#3b82f6', fillOpacity: .7, weight: 1 }).addTo(map);
      }
      announcedSteps = new Set();
      consecutiveOff = 0;
      // Pre-generate voice clips in parallel; do not block the route-display.
      preGenerateVoice(r.maneuvers);
      return r;
    }

    function updateTruck(latlng, heading) {
      if (!truckMarker) {
        const html = '<div style="width:30px;height:30px;border-radius:50%;background:#22c55e;border:3px solid #0f1419;display:flex;align-items:center;justify-content:center;color:#fff;font-size:1rem;box-shadow:0 0 0 4px rgba(34,197,94,.3);transition:transform .4s ease-out">🚚</div>';
        truckMarker = L.marker(latlng, { icon: L.divIcon({ className: '', html, iconSize: [30, 30], iconAnchor: [15, 15] }), zIndexOffset: 1000 }).addTo(map);
      } else {
        truckMarker.setLatLng(latlng);
      }
      try {
        const inner = truckMarker._icon && truckMarker._icon.firstChild;
        if (inner && heading != null) inner.style.transform = 'rotate(' + heading + 'deg)';
      } catch (_) {}
    }

    function speak(audioUrl) {
      if (!audioUrl) return;
      try {
        const a = new Audio(audioUrl);
        a.play().catch(() => {});
      } catch (_) {}
    }

    function processFix(fix) {
      if (!route || stopped) return;
      const ll = [fix.lat, fix.lng];
      updateTruck(ll, fix.heading);
      const np = nearestPointOnLine(ll, route.coords);

      // Off-route check — sustained drift triggers a reroute from the current position.
      if (np.distanceM > offRouteThresholdM) {
        consecutiveOff++;
        if (consecutiveOff >= offRouteFixesNeeded) {
          consecutiveOff = 0;
          loadRoute(ll, destination).then(r => onOffRoute(r)).catch(() => {});
          return;
        }
      } else {
        consecutiveOff = 0;
      }

      // Find the next un-announced maneuver and trigger it when we're within range.
      for (let i = 0; i < route.maneuvers.length; i++) {
        if (announcedSteps.has(i)) continue;
        const mv = route.maneuvers[i];
        const d = haversineM(ll, mv.location);
        if (d <= triggerWithinM) {
          announcedSteps.add(i);
          speak(mv.audioUrl);
          onTurn({ instruction: mv.verbalPre || mv.instruction, distanceM: d, stepIndex: i, totalSteps: route.maneuvers.length, maneuver: mv });
          break;
        }
      }

      // Arrival.
      const distToDest = haversineM(ll, destination);
      if (distToDest <= arriveWithinM) {
        onArrive({ distanceM: distToDest });
        if (simTimer) { clearInterval(simTimer); simTimer = null; }
        if (watchId != null) { try { navigator.geolocation.clearWatch(watchId); } catch (_) {} watchId = null; }
        stopped = true;
        return;
      }

      onProgress({ position: ll, snappedTo: np.snappedLatLng, distOffM: np.distanceM, distToDestM: distToDest });
    }

    function startSim(path) {
      simIdx = 0;
      simTimer = setInterval(() => {
        if (stopped || simIdx >= path.length) { clearInterval(simTimer); return; }
        const a = path[simIdx];
        const b = path[Math.min(simIdx + 1, path.length - 1)];
        const heading = bearing(a, b);
        processFix({ lat: a[0], lng: a[1], heading });
        simIdx++;
      }, simulatedTickMs);
    }

    function startReal() {
      if (!('geolocation' in navigator)) throw new Error('No geolocation in this browser');
      watchId = navigator.geolocation.watchPosition(
        (pos) => processFix({ lat: pos.coords.latitude, lng: pos.coords.longitude, heading: pos.coords.heading }),
        (err) => onOffRoute(err),
        { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
      );
    }

    function bearing(a, b) {
      if (!a || !b) return 0;
      const toRad = (d) => d * Math.PI / 180;
      const toDeg = (r) => r * 180 / Math.PI;
      const y = Math.sin(toRad(b[1] - a[1])) * Math.cos(toRad(b[0]));
      const x = Math.cos(toRad(a[0])) * Math.sin(toRad(b[0])) - Math.sin(toRad(a[0])) * Math.cos(toRad(b[0])) * Math.cos(toRad(b[1] - a[1]));
      return (toDeg(Math.atan2(y, x)) + 360) % 360;
    }

    (async function init() {
      try {
        await loadRoute(origin, destination);
        if (simulatedPath && simulatedPath.length) startSim(simulatedPath);
        else startReal();
      } catch (ex) {
        onOffRoute(ex);
      }
    })();

    return {
      stop() {
        stopped = true;
        try { if (watchId != null) navigator.geolocation.clearWatch(watchId); } catch (_) {}
        try { if (simTimer) clearInterval(simTimer); } catch (_) {}
      },
      route: () => route,
      currentTruck: () => truckMarker && truckMarker.getLatLng(),
    };
  }

  global.WolfsNav = { start };
})(typeof window !== 'undefined' ? window : globalThis);
