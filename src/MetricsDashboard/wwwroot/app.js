const endpoints = {
  regions: "/api/metrics/regions",
  substations: "/api/metrics/substations",
  sites: "/api/metrics/sites",
};

const refreshButton = document.getElementById("refresh");
const lastUpdated = document.getElementById("last-updated");

const grids = {
  regions: document.getElementById("regions-grid"),
  substations: document.getElementById("substations-grid"),
  sites: document.getElementById("sites-grid"),
};

const summarySparklineTargets = {
  regions: document.getElementById("regions-sparkline"),
  substations: document.getElementById("substations-sparkline"),
  sites: document.getElementById("sites-sparkline"),
};

const trendTargets = {
  energy: document.getElementById("trend-energy"),
  discharge: document.getElementById("trend-discharge"),
  charge: document.getElementById("trend-charge"),
  energyChart: document.getElementById("trend-energy-chart"),
  dischargeChart: document.getElementById("trend-discharge-chart"),
  chargeChart: document.getElementById("trend-charge-chart"),
};

refreshButton.addEventListener("click", () => {
  loadMetrics(true);
});

const pollIntervalMs = 1000;
const historyLimit = 30;

const histories = {
  regions: new Map(),
  substations: new Map(),
  sites: new Map(),
  summary: {
    regions: [],
    substations: [],
    sites: [],
  },
  trends: {
    energy: [],
    discharge: [],
    charge: [],
  },
};

const renderedHistories = new Map();

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

function pushHistory(list, value) {
  if (!Number.isFinite(value)) {
    return list;
  }
  list.push(value);
  if (list.length > historyLimit) {
    list.splice(0, list.length - historyLimit);
  }
  return list;
}

function pushMapHistory(map, key, value) {
  const list = map.get(key) || [];
  pushHistory(list, value);
  map.set(key, list);
  return list;
}

function sparklineSvg(values) {
  if (!values || values.length < 2) {
    return "";
  }
  const width = 120;
  const height = 40;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(1, max - min);
  const pad = 4;

  const points = values.map((value, index) => {
    const x = (index / (values.length - 1)) * (width - pad * 2) + pad;
    const normalized = (value - min) / range;
    const y = height - pad - normalized * (height - pad * 2);
    return [x, y];
  });

  const line = points.map((point, index) => `${index === 0 ? "M" : "L"} ${point[0]} ${point[1]}`).join(" ");
  const area = `${line} L ${width - pad} ${height - pad} L ${pad} ${height - pad} Z`;

  return `
    <svg class="sparkline" viewBox="0 0 ${width} ${height}" preserveAspectRatio="none" aria-hidden="true">
      <path class="sparkline-area" d="${area}"></path>
      <path d="${line}"></path>
    </svg>
  `;
}

function renderSparkline(target, values) {
  if (!target) {
    return;
  }
  const svg = sparklineSvg(values);
  target.innerHTML = svg || "";
}

function normalizeHistory(prev, next) {
  const maxLength = Math.max(prev.length, next.length);
  if (!maxLength) {
    return [[], []];
  }
  const firstPrev = prev[0] ?? next[0] ?? 0;
  const firstNext = next[0] ?? prev[0] ?? 0;
  const paddedPrev = [];
  const paddedNext = [];
  for (let i = 0; i < maxLength; i += 1) {
    paddedPrev.push(prev[i] ?? firstPrev);
    paddedNext.push(next[i] ?? firstNext);
  }
  return [paddedPrev, paddedNext];
}

function animateSparkline(target, values, key, duration = 650) {
  if (!target) {
    return;
  }
  if (!values || values.length < 2) {
    renderSparkline(target, values);
    return;
  }

  const previous = renderedHistories.get(key) || [];
  const [startValues, endValues] = normalizeHistory(previous, values);
  const start = performance.now();

  function step(now) {
    const elapsed = now - start;
    const t = Math.min(1, elapsed / duration);
    const eased = t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2;
    const blended = endValues.map((value, index) => {
      const from = startValues[index] ?? value;
      return from + (value - from) * eased;
    });
    renderSparkline(target, blended);
    if (t < 1) {
      requestAnimationFrame(step);
    }
  }

  renderedHistories.set(key, values.slice());
  requestAnimationFrame(step);
}

function pulse(element) {
  if (!element) {
    return;
  }
  element.classList.remove("pulse");
  // Force reflow to restart the animation.
  void element.offsetWidth;
  element.classList.add("pulse");
}

function metricBlock(label, role) {
  return `
    <div class="metric" data-metric="${role}">
      <div class="metric-header">
        <span>${label}</span>
        <span class="metric-value" data-role="${role}-value"></span>
      </div>
      <div class="meter"><span data-role="${role}-meter"></span></div>
    </div>
  `;
}

function createCard(item, config) {
  const card = document.createElement("article");
  card.className = "card";
  card.dataset.key = config.key(item);
  card.innerHTML = `
    <div class="card-header">
      <div>
        <div class="card-title">${config.prefix} <span data-role="id"></span></div>
        <div class="card-subtitle" data-role="subtitle"></div>
      </div>
      <div class="updated" data-role="updated"></div>
    </div>
    <div class="card-chart" data-role="sparkline"></div>
    <div class="metrics">
      ${metricBlock("Energy", "energy")}
      ${metricBlock("Discharge", "discharge")}
      ${metricBlock("Charge", "charge")}
      ${metricBlock("Confidence", "confidence")}
    </div>
    <div class="status-row">
      <span class="pill online" data-role="online"></span>
      <span class="pill offline" data-role="offline"></span>
    </div>
  `;
  return card;
}

function updateCard(card, item, config, maxes, historyMap) {
  const metrics = item.metrics;
  const key = config.key(item);
  const history = pushMapHistory(historyMap, key, metrics.availableEnergyKwh);

  card.querySelector("[data-role='id']").textContent = config.id(item);
  card.querySelector("[data-role='subtitle']").textContent = config.subtitle(item);
  card.querySelector("[data-role='updated']").textContent = formatRelativeTime(item.updatedAt);

  card.querySelector("[data-role='energy-value']").textContent = formatEnergy(
    metrics.availableEnergyKwh
  );
  card.querySelector("[data-role='discharge-value']").textContent = formatPower(
    metrics.availableDischargeKw
  );
  card.querySelector("[data-role='charge-value']").textContent = formatPower(
    metrics.availableChargeKw
  );
  card.querySelector("[data-role='confidence-value']").textContent = formatEnergy(
    metrics.confidenceWeightedEnergyKwh
  );

  card.querySelector("[data-role='energy-meter']").style.width = `${calcPercent(
    metrics.availableEnergyKwh,
    maxes.energy
  )}%`;
  card.querySelector("[data-role='discharge-meter']").style.width = `${calcPercent(
    metrics.availableDischargeKw,
    maxes.discharge
  )}%`;
  card.querySelector("[data-role='charge-meter']").style.width = `${calcPercent(
    metrics.availableChargeKw,
    maxes.charge
  )}%`;
  card.querySelector("[data-role='confidence-meter']").style.width = `${calcPercent(
    metrics.confidenceWeightedEnergyKwh,
    maxes.confidence
  )}%`;

  card.querySelector("[data-role='online']").textContent = `Online ${metrics.onlineCount}`;
  card.querySelector("[data-role='offline']").textContent = `Offline ${metrics.offlineCount}`;

  animateSparkline(
    card.querySelector("[data-role='sparkline']"),
    history,
    `card-${key}`
  );
  pulse(card);
}

function emptyCard(message) {
  return `
    <article class="card empty">
      <strong>${message}</strong>
      <div>Waiting for live telemetry to arrive.</div>
    </article>
  `;
}

function renderGroup(target, items, config, historyMap) {
  if (!items.length) {
    target.innerHTML = emptyCard(`No ${config.label} metrics yet`);
    return;
  }

  if (target.querySelector(".empty")) {
    target.innerHTML = "";
  }

  const maxes = {
    energy: Math.max(1, ...items.map((item) => item.metrics.availableEnergyKwh)),
    discharge: Math.max(1, ...items.map((item) => item.metrics.availableDischargeKw)),
    charge: Math.max(1, ...items.map((item) => item.metrics.availableChargeKw)),
    confidence: Math.max(1, ...items.map((item) => item.metrics.confidenceWeightedEnergyKwh)),
  };

  const existing = new Map(
    Array.from(target.querySelectorAll(".card[data-key]")).map((card) => [card.dataset.key, card])
  );
  const seen = new Set();

  items.forEach((item) => {
    const key = config.key(item);
    seen.add(key);
    let card = existing.get(key);
    if (!card) {
      card = createCard(item, config);
      target.appendChild(card);
    }
    updateCard(card, item, config, maxes, historyMap);
  });

  existing.forEach((card, key) => {
    if (!seen.has(key)) {
      card.remove();
    }
  });
}

function updateSummary(regions, substations, sites) {
  document.getElementById("regions-count").textContent = regions.length;
  document.getElementById("substations-count").textContent = substations.length;
  document.getElementById("sites-count").textContent = sites.length;

  const regionEnergy = sumBy(regions, (item) => item.metrics.availableEnergyKwh);
  const substationEnergy = sumBy(substations, (item) => item.metrics.availableEnergyKwh);
  const siteEnergy = sumBy(sites, (item) => item.metrics.availableEnergyKwh);

  document.getElementById("regions-total").textContent = `Energy ${formatEnergy(regionEnergy)}`;
  document.getElementById("substations-total").textContent = `Energy ${formatEnergy(
    substationEnergy
  )}`;
  document.getElementById("sites-total").textContent = `Energy ${formatEnergy(siteEnergy)}`;

  pushHistory(histories.summary.regions, regionEnergy);
  pushHistory(histories.summary.substations, substationEnergy);
  pushHistory(histories.summary.sites, siteEnergy);

  animateSparkline(
    summarySparklineTargets.regions,
    histories.summary.regions,
    "summary-regions"
  );
  animateSparkline(
    summarySparklineTargets.substations,
    histories.summary.substations,
    "summary-substations"
  );
  animateSparkline(
    summarySparklineTargets.sites,
    histories.summary.sites,
    "summary-sites"
  );

  pulse(document.querySelector(".summary-card:nth-child(1)"));
  pulse(document.querySelector(".summary-card:nth-child(2)"));
  pulse(document.querySelector(".summary-card:nth-child(3)"));
}

function updateTrends(regions, substations, sites) {
  const totalEnergy = sumBy(sites, (item) => item.metrics.availableEnergyKwh);
  const totalDischarge = sumBy(sites, (item) => item.metrics.availableDischargeKw);
  const totalCharge = sumBy(sites, (item) => item.metrics.availableChargeKw);

  trendTargets.energy.textContent = formatEnergy(totalEnergy);
  trendTargets.discharge.textContent = formatPower(totalDischarge);
  trendTargets.charge.textContent = formatPower(totalCharge);

  pushHistory(histories.trends.energy, totalEnergy);
  pushHistory(histories.trends.discharge, totalDischarge);
  pushHistory(histories.trends.charge, totalCharge);

  animateSparkline(trendTargets.energyChart, histories.trends.energy, "trend-energy");
  animateSparkline(
    trendTargets.dischargeChart,
    histories.trends.discharge,
    "trend-discharge"
  );
  animateSparkline(trendTargets.chargeChart, histories.trends.charge, "trend-charge");

  pulse(trendTargets.energyChart?.closest(".trend-card"));
  pulse(trendTargets.dischargeChart?.closest(".trend-card"));
  pulse(trendTargets.chargeChart?.closest(".trend-card"));
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

async function loadMetrics(forcePulse = false) {
  try {
    const [regions, substations, sites] = await Promise.all([
      fetchJson(endpoints.regions),
      fetchJson(endpoints.substations),
      fetchJson(endpoints.sites),
    ]);

    updateSummary(regions, substations, sites);
    updateTrends(regions, substations, sites);

    renderGroup(
      grids.regions,
      regions,
      {
        label: "regional",
        prefix: "Region",
        key: (item) => `region-${item.regionId}`,
        id: (item) => item.regionId,
        subtitle: () => "System-wide aggregate",
      },
      histories.regions
    );
    renderGroup(
      grids.substations,
      substations,
      {
        label: "substation",
        prefix: "Substation",
        key: (item) => `substation-${item.substationId}`,
        id: (item) => item.substationId,
        subtitle: (item) => `Region ${item.regionId}`,
      },
      histories.substations
    );
    renderGroup(
      grids.sites,
      sites,
      {
        label: "site",
        prefix: "Site",
        key: (item) => `site-${item.siteId}`,
        id: (item) => item.siteId,
        subtitle: (item) => `Substation ${item.substationId}`,
      },
      histories.sites
    );
    updateLastUpdated(regions, substations, sites);

    if (forcePulse) {
      document.querySelectorAll(".card, .summary-card, .trend-card").forEach(pulse);
    }
  } catch (error) {
    console.error(error);
    lastUpdated.textContent = "error loading data";
  }
}

loadMetrics(true);
setInterval(loadMetrics, pollIntervalMs);
