window.renderStatusChart = function renderStatusChart(elementId, points) {
    const element = document.getElementById(elementId);
    if (!element) return;

    const width = 700;
    const height = 240;
    const pad = 28;
    const chartH = height - pad * 2;
    const chartW = width - pad * 2;

    // Empty state
    if (!points || points.length === 0) {
        element.setAttribute("viewBox", `0 0 ${width} ${height}`);
        element.innerHTML = `
            <text x="${width / 2}" y="${height / 2}" text-anchor="middle"
                  fill="rgba(255,255,255,0.2)" font-family="var(--font-body, sans-serif)" font-size="14">
                No data yet
            </text>`;
        return;
    }

    // Handle both PascalCase (C# default) and camelCase property names
    const val = p => Number(p.value ?? p.Value ?? 0);
    const values = points.map(val);
    const rawMin = Math.min(...values);
    const rawMax = Math.max(...values);
    const rawRange = rawMax - rawMin;

    // Scale the vertical axis
    let dispMin, dispMax;
    if (rawRange === 0) {
        // All values identical: range from 0 to value × 1.25 so the line
        // sits at ~80% up from the bottom instead of dead center
        dispMin = 0;
        dispMax = Math.max(rawMax * 1.25, 1);
    } else {
        // Varied data: add 12% breathing room above and below
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
                    stroke="rgba(255,255,255,0.04)" stroke-width="1" stroke-dasharray="3 6"/>`;
    }).join("");

    // Dots: show for actual data points only (not the duplicated extension point)
    let dots = "";
    if (points.length <= 20) {
        const r = points.length <= 5 ? 4.5 : 3;
        dots = points.map((_, i) => {
            const c = coords[i];
            return `
                <circle cx="${c.x}" cy="${c.y}" r="${r + 4}" fill="rgb(45,212,191)" opacity="0.15"/>
                <circle cx="${c.x}" cy="${c.y}" r="${r}" fill="rgb(45,212,191)"/>
            `;
        }).join("");
    }

    // Value labels for very small datasets
    let labels = "";
    if (points.length <= 5) {
        labels = points.map((p, i) => {
            const c = coords[i];
            const v = val(p);
            const display = v % 1 === 0 ? v.toString() : v.toFixed(1);
            return `<text x="${c.x}" y="${c.y - 14}" text-anchor="middle"
                        fill="rgba(255,255,255,0.5)" font-family="var(--font-mono, monospace)" font-size="11">
                        ${display}
                    </text>`;
        }).join("");
    }

    element.setAttribute("viewBox", `0 0 ${width} ${height}`);
    element.innerHTML = `
        <defs>
            <linearGradient id="areaGrad-${elementId}" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="rgb(45,212,191)" stop-opacity="0.18"/>
                <stop offset="100%" stop-color="rgb(45,212,191)" stop-opacity="0"/>
            </linearGradient>
        </defs>
        ${gridLines}
        <line x1="${pad}" y1="${height - pad}" x2="${pad + chartW}" y2="${height - pad}"
              stroke="rgba(255,255,255,0.06)" stroke-width="1"/>
        <path d="${areaPath}" fill="url(#areaGrad-${elementId})"/>
        <path class="chart-line" d="${linePath}"/>
        ${dots}
        ${labels}
    `;
};
