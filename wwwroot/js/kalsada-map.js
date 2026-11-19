// Leaflet maps for KalsadaWatch. Loaded once from App.razor; Blazor calls these through IJSRuntime.
//   kalsadaMap    - the Road Damage Map on the Verify page
//   kalsadaPicker - the "pin the location" map on the Report page

const KALSADA_CENTER = [14.5995, 120.9842]; // city center (Manila)

// OpenStreetMap tiles (free, no API key). CARTO's tiles now require a key.
function kalsadaAddTiles(map) {
  L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    maxZoom: 19
  }).addTo(map);
}

// The teardrop pin used on both maps.
function kalsadaPinIcon(color) {
  return L.divIcon({
    className: 'custom-map-pin',
    html: `
      <div style="
        width: 32px;
        height: 32px;
        border-radius: 50% 50% 50% 0;
        background: ${color};
        transform: rotate(-45deg);
        display: flex;
        align-items: center;
        justify-content: center;
        border: 2.5px solid #FFFFFF;
        box-shadow: 0 4px 10px rgba(0,0,0,0.25);
        cursor: pointer;
        transition: transform 0.2s ease;
      ">
        <div style="
          width: 10px;
          height: 10px;
          border-radius: 50%;
          background: #FFFFFF;
        "></div>
      </div>
    `,
    iconSize: [32, 32],
    iconAnchor: [16, 32],
    popupAnchor: [0, -32]
  });
}

window.kalsadaMap = {
  mapInstance: null,
  markersGroup: null,
  heatmapGroup: null,
  latest: null, // data that arrived before the map finished loading

  // Report text comes from citizens, so escape it before putting it inside popup HTML.
  escapeHtml: function (value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  },

  init: function (elementId, reportsJson) {
    if (typeof L === 'undefined') {
      setTimeout(() => window.kalsadaMap.init(elementId, reportsJson), 200);
      return;
    }

    const container = document.getElementById(elementId);
    if (!container) return;

    if (window.kalsadaMap.mapInstance) {
      window.kalsadaMap.mapInstance.remove();
    }

    // Initialize map centered on City Center (Manila coordinates)
    const map = L.map(elementId, {
      scrollWheelZoom: true,
      zoomControl: true
    }).setView(KALSADA_CENTER, 13);

    window.kalsadaMap.mapInstance = map;
    kalsadaAddTiles(map);

    window.kalsadaMap.markersGroup = L.layerGroup().addTo(map);
    window.kalsadaMap.heatmapGroup = L.layerGroup().addTo(map);

    const latest = window.kalsadaMap.latest;
    window.kalsadaMap.latest = null;
    if (latest) {
      window.kalsadaMap.renderMarkers(latest.reports, latest.showHeatmap);
    } else {
      window.kalsadaMap.renderMarkers(JSON.parse(reportsJson), false);
    }
  },

  renderMarkers: function (reports, showHeatmap) {
    if (!window.kalsadaMap.mapInstance) return;

    window.kalsadaMap.markersGroup.clearLayers();
    window.kalsadaMap.heatmapGroup.clearLayers();

    reports.forEach(r => {
      // Reports without a pinned location are listed on the page but cannot be drawn on the map.
      if (r.Lat == null || r.Lng == null) return;

      // Status color mapping
      let color = '#2563EB'; // Blue (Inspection)
      let label = 'Under Inspection';
      if (r.Status === 'Under Construction') {
        color = '#F59E0B'; // Amber
        label = 'Under Construction';
      } else if (r.Status === 'Finished') {
        color = '#10B981'; // Emerald
        label = 'Finished & Repaired';
      }

      const esc = window.kalsadaMap.escapeHtml;

      const svgIcon = kalsadaPinIcon(color);

      const marker = L.marker([r.Lat, r.Lng], { icon: svgIcon });
      
      // Rich Popup Window
      const popupContent = `
        <div style="font-family: 'Plus Jakarta Sans', sans-serif; padding: 4px; min-width: 200px;">
          <div style="font-size: 10px; font-weight: 700; text-transform: uppercase; color: ${color}; margin-bottom: 2px;">
            ● ${label}${r.ProgressPercent != null ? ` (${r.ProgressPercent}%)` : ''}
          </div>
          <div style="font-size: 13px; font-weight: 700; color: #0F172A; margin-bottom: 4px;">
            ${esc(r.Title)}
          </div>
          <div style="font-size: 11px; color: #64748B; margin-bottom: 6px;">
            📍 ${esc(r.Location)}
          </div>
          <div style="font-size: 11px; background: #F1F5F9; padding: 4px 8px; border-radius: 6px; color: #334155;">
            District: <strong>${esc(r.District)}</strong>
          </div>
        </div>
      `;

      marker.bindPopup(popupContent);
      window.kalsadaMap.markersGroup.addLayer(marker);

      // Heatmap density visualization (radial pulsing gradient circles)
      if (showHeatmap) {
        const heatColor = (r.Status === 'Under Construction' || r.Severity === 'High') ? '#EF4444' : '#F59E0B';
        const radius = (r.Severity === 'High') ? 450 : 320;
        
        const heatCircle = L.circle([r.Lat, r.Lng], {
          radius: radius,
          color: heatColor,
          fillColor: heatColor,
          fillOpacity: 0.28,
          weight: 1.5,
          dashArray: '4, 4'
        });

        window.kalsadaMap.heatmapGroup.addLayer(heatCircle);
      }
    });
  },

  updateData: function (filteredReportsJson, showHeatmap) {
    const reports = JSON.parse(filteredReportsJson);
    if (!window.kalsadaMap.mapInstance) {
      // The map is still loading: keep the data and draw it when init finishes.
      window.kalsadaMap.latest = { reports: reports, showHeatmap: showHeatmap };
      return;
    }
    window.kalsadaMap.renderMarkers(reports, showHeatmap);
  },

  panToLocation: function (lat, lng, title, status, id) {
    if (!window.kalsadaMap.mapInstance) return;
    window.kalsadaMap.mapInstance.flyTo([lat, lng], 16, {
      duration: 1.2,
      easeLinearity: 0.25
    });
  },

  resetView: function () {
    if (!window.kalsadaMap.mapInstance) return;
    window.kalsadaMap.mapInstance.flyTo(KALSADA_CENTER, 13, { duration: 1 });
  }
};

// Location picker on the Report page. The citizen clicks the map (or uses their phone's GPS)
// to pin the hazard, and the coordinates are sent back to Report.razor.
window.kalsadaPicker = {
  map: null,
  marker: null,
  dotNet: null, // reference to the Report component, for sending the pin back

  init: function (elementId, dotNetRef) {
    const picker = window.kalsadaPicker;
    const container = document.getElementById(elementId);
    if (!container || typeof L === 'undefined') return;

    picker.dispose();
    picker.dotNet = dotNetRef;

    const map = L.map(elementId, { scrollWheelZoom: true }).setView(KALSADA_CENTER, 13);
    kalsadaAddTiles(map);
    map.on('click', e => picker.setPin(e.latlng.lat, e.latlng.lng, false));
    picker.map = map;
  },

  setPin: function (lat, lng, zoomTo) {
    const picker = window.kalsadaPicker;
    if (!picker.map) return;

    if (picker.marker) {
      picker.marker.setLatLng([lat, lng]);
    } else {
      picker.marker = L.marker([lat, lng], { icon: kalsadaPinIcon('#DC2626') }).addTo(picker.map);
    }
    if (zoomTo) picker.map.flyTo([lat, lng], 17, { duration: 1 });
    if (picker.dotNet) picker.dotNet.invokeMethodAsync('SetPinnedLocation', lat, lng);
  },

  useMyLocation: function () {
    const picker = window.kalsadaPicker;
    if (!navigator.geolocation) {
      picker.reportProblem('This browser cannot share its location. Click the map instead.');
      return;
    }
    navigator.geolocation.getCurrentPosition(
      position => picker.setPin(position.coords.latitude, position.coords.longitude, true),
      () => picker.reportProblem('Could not get your location. Allow location access, or click the map instead.'),
      { enableHighAccuracy: true, timeout: 10000 }
    );
  },

  reportProblem: function (message) {
    const picker = window.kalsadaPicker;
    if (picker.dotNet) picker.dotNet.invokeMethodAsync('LocationFailed', message);
  },

  clear: function () {
    const picker = window.kalsadaPicker;
    if (picker.marker) {
      picker.marker.remove();
      picker.marker = null;
    }
    if (picker.map) picker.map.setView(KALSADA_CENTER, 13);
  },

  dispose: function () {
    const picker = window.kalsadaPicker;
    if (picker.map) picker.map.remove();
    picker.map = null;
    picker.marker = null;
    picker.dotNet = null;
  }
};
