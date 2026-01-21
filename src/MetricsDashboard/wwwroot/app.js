const endpoints = {
  regions: "/api/metrics/regions",
  substations: "/api/metrics/substations",
  sites: "/api/metrics/sites",
};

const refreshButton = document.getElementById("refresh");
const lastUpdated = document.getElementById("last-updated");

refreshButton.addEventListener("click", () => {
  loadMetrics();
});

const pollIntervalMs = 60000;

function formatNumber(value, decimals) {
  if (!Number.isFinite(value)) {
    return "0";
  }
  return value.toLocaleString(undefined, {
    minimumFractionDigits: 0,
    maximumFractionDigits: decimals,
  });
}

function formatEnergy(value) {
  return `${formatNumber(value, 1)} kWh`;
}

function formatPower(value) {
  return `${formatNumber(value, 0)} kW`;
}

function formatRelativeTime(value) {
  if (!value) {
    return "no data";
  }
  const timestamp = new Date(value).getTime();
  if (Number.isNaN(timestamp)) {
    return "unknown";
  }
  const deltaMs = Date.now() - timestamp;
  if (deltaMs < 10000) {
    return "just now";
  }
  const minutes = Math.round(deltaMs / 60000);
  if (minutes < 60) {
    return `${minutes}m ago`;
  }
  const hours = Math.round(minutes / 60);
  if (hours < 24) {
    return `${hours}h ago`;
  }
  const days = Math.round(hours / 24);
  return `${days}d ago`;
}

function calcPercent(value, max) {
  if (!Number.isFinite(value) || !Number.isFinite(max) || max <= 0) {
    return 0;
  }
  return Math.min(100, Math.round((value / max) * 100));
}

function sumBy(list, selector) {
  return list.reduce((total, item) => total + selector(item), 0);
}

function metricRow(label, value, percent) {
  return `
    <div class="metric">
      <div class="metric-header">
        <span>${label}</span>
        <span class="metric-value">${value}</span>
      </div>
      <div class="meter"><span style="width: ${percent}%"></span></div>
    </div>
  `;
}

function emptyCard(message) {
  return `
    <article class="card empty">
      <strong>${message}</strong>
      <div>Waiting for live telemetry to arrive.</div>
    </article>
  `;
}

function renderGroup(targetId, items, config) {
  const target = document.getElementById(targetId);
  if (!items.length) {
    target.innerHTML = emptyCard(`No ${config.label} metrics yet`);
    return;
  }

  const maxEnergy = Math.max(1, ...items.map((item) => item.metrics.availableEnergyKwh));
  const maxDischarge = Math.max(1, ...items.map((item) => item.metrics.availableDischargeKw));
  const maxCharge = Math.max(1, ...items.map((item) => item.metrics.availableChargeKw));
  const maxConfidence = Math.max(
    1,
    ...items.map((item) => item.metrics.confidenceWeightedEnergyKwh)
  );

  target.innerHTML = items
    .map((item, index) => {
      const id = config.id(item);
      const subtitle = config.subtitle(item);
      const metrics = item.metrics;
      const energy = metricRow(
        "Energy",
        formatEnergy(metrics.availableEnergyKwh),
        calcPercent(metrics.availableEnergyKwh, maxEnergy)
      );
      const discharge = metricRow(
        "Discharge",
        formatPower(metrics.availableDischargeKw),
        calcPercent(metrics.availableDischargeKw, maxDischarge)
      );
      const charge = metricRow(
        "Charge",
        formatPower(metrics.availableChargeKw),
        calcPercent(metrics.availableChargeKw, maxCharge)
      );
      const confidence = metricRow(
        "Confidence",
        formatEnergy(metrics.confidenceWeightedEnergyKwh),
        calcPercent(metrics.confidenceWeightedEnergyKwh, maxConfidence)
      );

      return `
        <article class="card" style="animation-delay:${Math.min(index, 6) * 70}ms">
          <div class="card-header">
            <div>
              <div class="card-title">${config.prefix} <span>${id}</span></div>
              <div class="card-subtitle">${subtitle}</div>
            </div>
            <div class="updated">${formatRelativeTime(item.updatedAt)}</div>
          </div>
          <div class="metrics">
            ${energy}
            ${discharge}
            ${charge}
            ${confidence}
          </div>
          <div class="status-row">
            <span class="pill online">Online ${metrics.onlineCount}</span>
            <span class="pill offline">Offline ${metrics.offlineCount}</span>
          </div>
        </article>
      `;
    })
    .join("");
}

function updateSummary(regions, substations, sites) {
  document.getElementById("regions-count").textContent = regions.length;
  document.getElementById("substations-count").textContent = substations.length;
  document.getElementById("sites-count").textContent = sites.length;

  const regionEnergy = sumBy(regions, (item) => item.metrics.availableEnergyKwh);
  const substationEnergy = sumBy(substations, (item) => item.metrics.availableEnergyKwh);
  const siteEnergy = sumBy(sites, (item) => item.metrics.availableEnergyKwh);

  document.getElementById("regions-total").textContent = `Energy ${formatEnergy(regionEnergy)}`;
  document.getElementById("substations-total").textContent = `Energy ${formatEnergy(substationEnergy)}`;
  document.getElementById("sites-total").textContent = `Energy ${formatEnergy(siteEnergy)}`;
}

function updateLastUpdated(regions, substations, sites) {
  const timestamps = [...regions, ...substations, ...sites]
    .map((item) => new Date(item.updatedAt).getTime())
    .filter((value) => Number.isFinite(value));
  if (!timestamps.length) {
    lastUpdated.textContent = "waiting for data";
    return;
  }
  const latest = new Date(Math.max(...timestamps));
  const timeLabel = latest.toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
  lastUpdated.textContent = `${timeLabel} (${formatRelativeTime(latest)})`;
}

async function fetchJson(url) {
  const response = await fetch(url, { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}

async function loadMetrics() {
  try {
    const [regions, substations, sites] = await Promise.all([
      fetchJson(endpoints.regions),
      fetchJson(endpoints.substations),
      fetchJson(endpoints.sites),
    ]);

    updateSummary(regions, substations, sites);
    renderGroup("regions-grid", regions, {
      label: "regional",
      prefix: "Region",
      id: (item) => item.regionId,
      subtitle: () => "System-wide aggregate",
    });
    renderGroup("substations-grid", substations, {
      label: "substation",
      prefix: "Substation",
      id: (item) => item.substationId,
      subtitle: (item) => `Region ${item.regionId}`,
    });
    renderGroup("sites-grid", sites, {
      label: "site",
      prefix: "Site",
      id: (item) => item.siteId,
      subtitle: (item) => `Substation ${item.substationId}`,
    });
    updateLastUpdated(regions, substations, sites);
  } catch (error) {
    console.error(error);
    lastUpdated.textContent = "error loading data";
  }
}

loadMetrics();
setInterval(loadMetrics, pollIntervalMs);
