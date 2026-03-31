const findTooltipTarget = target =>
    target instanceof Element ? target.closest("[data-tooltip]") : null;

const tooltipState = {
    element: null,
    activeTarget: null,
    initialized: false
};

const ensureTooltipElement = () => {
    if (tooltipState.element) {
        return tooltipState.element;
    }

    const tooltip = document.createElement("div");
    tooltip.className = "status-tooltip";
    tooltip.setAttribute("role", "tooltip");
    document.body.appendChild(tooltip);
    tooltipState.element = tooltip;
    return tooltip;
};

const decodeTooltipText = value => value
    .replaceAll("&#10;", "\n")
    .replaceAll("&quot;", "\"")
    .replaceAll("&#39;", "'")
    .replaceAll("&lt;", "<")
    .replaceAll("&gt;", ">")
    .replaceAll("&amp;", "&");

const positionTooltip = target => {
    const tooltip = ensureTooltipElement();
    const rect = target.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    let top = rect.top - tooltipRect.height - 10;

    if (top < 8) {
        top = rect.bottom + 10;
    }

    let left = rect.left + rect.width / 2 - tooltipRect.width / 2;
    left = Math.max(8, Math.min(left, window.innerWidth - tooltipRect.width - 8));

    tooltip.style.transform = `translate(${Math.round(left)}px, ${Math.round(top)}px)`;
};

const showTooltip = target => {
    const tooltip = ensureTooltipElement();
    const tooltipText = decodeTooltipText(target.getAttribute("data-tooltip") ?? "");
    if (!tooltipText) {
        return;
    }

    tooltip.textContent = tooltipText;
    tooltip.setAttribute("data-visible", "true");
    tooltipState.activeTarget = target;
    positionTooltip(target);
};

const hideTooltip = () => {
    if (!tooltipState.element) {
        return;
    }

    tooltipState.element.removeAttribute("data-visible");
    tooltipState.activeTarget = null;
};

const initializeStatusTooltips = () => {
    if (tooltipState.initialized) {
        return;
    }

    tooltipState.initialized = true;

    document.addEventListener("mouseover", event => {
        const target = findTooltipTarget(event.target);
        const relatedTarget = findTooltipTarget(event.relatedTarget);
        if (!target || target === relatedTarget) {
            return;
        }

        showTooltip(target);
    });

    document.addEventListener("mouseout", event => {
        const target = findTooltipTarget(event.target);
        const relatedTarget = findTooltipTarget(event.relatedTarget);
        if (!target || target === relatedTarget || tooltipState.activeTarget !== target) {
            return;
        }

        hideTooltip();
    });

    document.addEventListener("focusin", event => {
        const target = findTooltipTarget(event.target);
        if (target) {
            showTooltip(target);
        }
    });

    document.addEventListener("focusout", event => {
        const target = findTooltipTarget(event.target);
        if (target && tooltipState.activeTarget === target) {
            hideTooltip();
        }
    });

    window.addEventListener("scroll", hideTooltip, true);
    window.addEventListener("resize", hideTooltip);
};

initializeStatusTooltips();

window.renderStatusChart = function renderStatusChart(elementId, points, options = {}) {
    const element = document.getElementById(elementId);
    if (!element) return;

    const width = 700;
    const height = 240;
    const pad = 28;
    const chartH = height - pad * 2;
    const chartW = width - pad * 2;

    // Obsidian palette
    const primaryRGB = "123, 138, 255";
    const gridColor = "rgba(255, 255, 255, 0.03)";
    const textColor = "rgba(228, 228, 236, 0.35)";

    // Empty state
    if (!points || points.length === 0) {
        element.setAttribute("viewBox", `0 0 ${width} ${height}`);
        element.innerHTML = `
            <text x="${width / 2}" y="${height / 2}" text-anchor="middle"
                  fill="${textColor}" font-family="var(--font-family-body, sans-serif)" font-size="13">
                No data yet
            </text>`;
        return;
    }

    // Handle both PascalCase (C# default) and camelCase property names
    const val = p => Number(p.value ?? p.Value ?? 0);
    const label = p => String(p.label ?? p.Label ?? "");
    const formatValue = value => {
        if (typeof options.decimals === "number") {
            return value.toFixed(options.decimals);
        }

        return Number.isInteger(value) ? value.toString() : value.toFixed(1);
    };
    const escapeHtml = value => value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;");
    const escapeAttribute = value => value
        .replaceAll("&", "&amp;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\n", "&#10;");
    const tooltipText = p => {
        const segments = [];
        const pointLabel = label(p);
        if (pointLabel.length > 0) {
            segments.push(pointLabel);
        }

        segments.push(`${formatValue(val(p))}${options.valueSuffix ?? ""}`);
        return escapeHtml(segments.join("\n"));
    };
    const values = points.map(val);
    const rawMin = Math.min(...values);
    const rawMax = Math.max(...values);
    const rawRange = rawMax - rawMin;

    // Scale the vertical axis
    let dispMin, dispMax;
    if (rawRange === 0) {
        dispMin = 0;
        dispMax = Math.max(rawMax * 1.25, 1);
    } else {
        const vPad = rawRange * 0.12;
        dispMin = rawMin - vPad;
        dispMax = rawMax + vPad;
    }
    const dispRange = dispMax - dispMin;

    const stepX = points.length <= 1 ? 0 : chartW / (points.length - 1);

    const coords = points.map((p, i) => ({
        x: pad + i * stepX,
        y: pad + (1 - (val(p) - dispMin) / dispRange) * chartH
    }));

    // Single point: extend to a flat line spanning the chart
    if (coords.length === 1) {
        coords.push({ x: pad + chartW, y: coords[0].y });
    }

    const linePath = coords.map((c, i) =>
        `${i === 0 ? "M" : "L"} ${c.x} ${c.y}`
    ).join(" ");

    const areaPath = linePath
        + ` L ${coords[coords.length - 1].x} ${height - pad}`
        + ` L ${coords[0].x} ${height - pad} Z`;

    const gridLines = [0.25, 0.5, 0.75].map(pct => {
        const y = Math.round(pad + chartH * pct);
        return `<line x1="${pad}" y1="${y}" x2="${pad + chartW}" y2="${y}"
                    stroke="${gridColor}" stroke-width="1" stroke-dasharray="4 8"/>`;
    }).join("");

    // Dots: show for actual data points only
    let dots = "";
    if (points.length <= 20) {
        const r = points.length <= 5 ? 4 : 2.5;
        dots = points.map((p, i) => {
            const c = coords[i];
            const pointTooltip = escapeAttribute(tooltipText(p));
            return `
                <circle cx="${c.x}" cy="${c.y}" r="${r + 5}" fill="rgb(${primaryRGB})" opacity="0.08"/>
                <circle cx="${c.x}" cy="${c.y}" r="${r}" fill="rgb(${primaryRGB})" stroke="rgba(6,6,11,0.6)" stroke-width="1.5"
                        data-tooltip="${pointTooltip}" tabindex="0" aria-label="${pointTooltip}"></circle>
            `;
        }).join("");
    }

    const hoverTargets = points.map((p, i) => {
        const c = coords[i];
        const pointTooltip = escapeAttribute(tooltipText(p));
        return `
            <circle cx="${c.x}" cy="${c.y}" r="10" fill="rgba(0, 0, 0, 0.001)"
                    data-tooltip="${pointTooltip}" tabindex="0" aria-label="${pointTooltip}"></circle>
        `;
    }).join("");

    // Value labels for very small datasets
    let labels = "";
    if (points.length <= 5) {
        labels = points.map((p, i) => {
            const c = coords[i];
            return `<text x="${c.x}" y="${c.y - 14}" text-anchor="middle"
                        fill="${textColor}" font-family="var(--font-family-mono, monospace)" font-size="10.5">
                        ${formatValue(val(p))}
                    </text>`;
        }).join("");
    }

    element.setAttribute("viewBox", `0 0 ${width} ${height}`);
    element.innerHTML = `
        <defs>
            <linearGradient id="areaGrad-${elementId}" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="rgb(${primaryRGB})" stop-opacity="0.12"/>
                <stop offset="100%" stop-color="rgb(${primaryRGB})" stop-opacity="0"/>
            </linearGradient>
        </defs>
        ${gridLines}
        <line x1="${pad}" y1="${height - pad}" x2="${pad + chartW}" y2="${height - pad}"
              stroke="${gridColor}" stroke-width="1"/>
        <path d="${areaPath}" fill="url(#areaGrad-${elementId})"/>
        <path class="chart-line" d="${linePath}"/>
        ${dots}
        ${hoverTargets}
        ${labels}
    `;
};
